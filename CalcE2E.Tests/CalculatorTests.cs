using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace CalcE2E.Tests;

[TestFixture]
public class CalculatorTests
{
    // Single source of truth for process names — used in both the kill pass and the wait pass.
    private static readonly string[] CalcProcessNames = ["CalculatorApp", "Calculator"];
    private const int KillWaitMs      = 2_000;
    private const int PollIntervalMs  = 250;
    private const int PollMaxAttempts = 20;   // 20 × 250 ms = 5 s total
    private const int WindowTimeoutS  = 5;

    private Application      _app        = null!;
    private UIA3Automation   _automation = null!;
    private Window           _window     = null!;
    private CalculatorScreen _calc       = null!;

    [SetUp]
    public void SetUp()
    {
        KillOrphanedCalculators();

        // calc.exe is a thin launcher that immediately spawns CalculatorApp.exe and exits.
        // We start it via the shell, poll until the real host process appears, then attach.
        Process.Start(new ProcessStartInfo("calc.exe") { UseShellExecute = true });

        _automation = new UIA3Automation();

        var host = WaitForCalculatorProcess();
        Assert.That(host, Is.Not.Null, "Calculator did not start within 5 seconds.");

        _app    = Application.Attach(host!);
        _window = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(WindowTimeoutS))!;
        Assert.That(_window, Is.Not.Null, "Calculator main window did not appear.");

        _calc = new CalculatorScreen(_window);
    }

    [TearDown]
    public void TearDown()
    {
        try { _app?.Close(); } catch { /* ignore */ }
        _automation?.Dispose();
    }

    private static void KillOrphanedCalculators()
    {
        foreach (var p in CalcProcessNames.SelectMany(Process.GetProcessesByName))
            try { p.Kill(); p.WaitForExit(KillWaitMs); } catch { /* already gone */ }
    }

    private static Process? WaitForCalculatorProcess()
    {
        for (var i = 0; i < PollMaxAttempts; i++)
        {
            var host = CalcProcessNames
                .SelectMany(Process.GetProcessesByName)
                .FirstOrDefault();
            if (host is not null) return host;
            Thread.Sleep(PollIntervalMs);
        }
        return null;
    }

    // ── Addition ──────────────────────────────────────────────────────────────

    [TestCase(2, 3,  "5")]
    [TestCase(0, 0,  "0")]
    public void Addition_TwoOperands_ReturnsSum(int a, int b, string expected)
    {
        _calc.Enter(a).Plus().Enter(b).Equal();
        Assert.That(_calc.Result, Does.Contain(expected));
    }

    // ── Subtraction ───────────────────────────────────────────────────────────

    [TestCase(10, 3,  "7")]
    [TestCase( 5, 5,  "0")]
    [TestCase( 1, 9, "-8")]
    public void Subtraction_TwoOperands_ReturnsDifference(int a, int b, string expected)
    {
        _calc.Enter(a).Minus().Enter(b).Equal();
        Assert.That(_calc.Result, Does.Contain(expected));
    }

    // ── Multiplication ────────────────────────────────────────────────────────

    [TestCase(3, 4, "12")]
    [TestCase(0, 7,  "0")]
    [TestCase(9, 9, "81")]
    public void Multiplication_TwoOperands_ReturnsProduct(int a, int b, string expected)
    {
        _calc.Enter(a).Multiply().Enter(b).Equal();
        Assert.That(_calc.Result, Does.Contain(expected));
    }

    // ── Division ──────────────────────────────────────────────────────────────

    [TestCase(10, 2, "5")]
    [TestCase( 0, 5, "0")]   // zero dividend — distinct from divide-by-zero
    public void IntegerDivision_TwoOperands_ReturnsQuotient(int a, int b, string expected)
    {
        _calc.Enter(a).Divide().Enter(b).Equal();
        Assert.That(_calc.Result, Does.Contain(expected));
    }

    [Test]
    public void Division_OneByThree_DisplaysRepeatingDecimal()
    {
        _calc.Digit(1).Divide().Digit(3).Equal();

        // Decimal separator is locale-dependent: '.' in en-US, ',' in many EU locales.
        Assert.That(
            _calc.Result,
            Does.StartWith("0.3333").Or.StartWith("0,3333"),
            "Expected repeating decimal 0.3333… or 0,3333…"
        );
    }

    [Test]
    public void Division_ByZero_DisplaysErrorMessage()
    {
        _calc.Digit(1).Divide().Digit(0).Equal();
        Assert.That(_calc.Result, Does.Contain("Cannot divide by zero"));
    }

    [Test]
    public void Division_ZeroByZero_DisplaysUndefinedMessage()
    {
        // 0 ÷ 0 is a distinct case from n ÷ 0; Calculator reports "Result is undefined".
        _calc.Digit(0).Divide().Digit(0).Equal();
        Assert.That(_calc.Result, Does.Contain("undefined"));
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [Test]
    public void Clear_AfterCalculation_ResetsDisplayToZero()
    {
        _calc.Digit(7).Plus().Digit(8).Equal();
        _calc.Clear();
        Assert.That(_calc.Result, Is.EqualTo("0"));
    }

    [Test]
    public void ClearEntry_DuringOperation_PreservesFirstOperandAndOperator()
    {
        // 5 + [type 9] [CE] [type 3] = → should compute 5 + 3 = 8, not 5 + 9 = 14.
        // CE wipes the current entry only; C would wipe everything.
        _calc.Enter(5).Plus().Enter(9).ClearEntry().Enter(3).Equal();
        Assert.That(_calc.Result, Does.Contain("8"));
    }

    // ── Chained operations ────────────────────────────────────────────────────

    [Test]
    public void ChainedOperations_ResultCarriesIntoNextCalculation()
    {
        // 3 + 4 = 7, then − 2 = 5; no Clear between operations.
        _calc.Enter(3).Plus().Enter(4).Equal().Minus().Enter(2).Equal();
        Assert.That(_calc.Result, Does.Contain("5"));
    }

    [Test]
    public void OperatorChange_LastOperatorIsApplied()
    {
        // Press + then immediately × before entering the second operand.
        // Only the final operator should be used: 5 × 4 = 20.
        _calc.Enter(5).Plus().Multiply().Enter(4).Equal();
        Assert.That(_calc.Result, Does.Contain("20"));
    }

    [Test]
    public void RepeatedEquals_ReappliesLastOperation()
    {
        // 5 + 3 = 8; each subsequent = re-adds 3: 11, then 14.
        _calc.Enter(5).Plus().Enter(3).Equal().Equal().Equal();
        Assert.That(_calc.Result, Does.Contain("14"));
    }

    // ── Decimal input ─────────────────────────────────────────────────────────

    [Test]
    public void Addition_DecimalOperands_ReturnsExactSum()
    {
        // 1.5 + 2.5 = 4 — also exercises the decimal-point button.
        _calc.Digit(1).DecimalPoint().Digit(5)
             .Plus()
             .Digit(2).DecimalPoint().Digit(5)
             .Equal();
        Assert.That(_calc.Result, Does.Contain("4"));
    }

    [Test]
    public void Addition_PointOneAndPointNine_EqualsOne()
    {
        // 0.1 + 0.9: raw IEEE-754 floating point gives 0.999…;
        // Calculator is expected to round correctly and display 1.
        _calc.Digit(0).DecimalPoint().Digit(1)
             .Plus()
             .Digit(0).DecimalPoint().Digit(9)
             .Equal();
        Assert.That(_calc.Result, Does.Contain("1"));
    }

    [Test]
    public void Multiplication_DecimalByInteger_ReturnsProduct()
    {
        // 0.5 × 6 = 3
        _calc.Digit(0).DecimalPoint().Digit(5).Multiply().Digit(6).Equal();
        Assert.That(_calc.Result, Does.Contain("3"));
    }

    // ── Negate (±) ───────────────────────────────────────────────────────────

    [Test]
    public void Negate_PositiveNumber_FlipsSign()
    {
        _calc.Enter(7).Negate();
        Assert.That(_calc.Result, Does.Contain("-7"));
    }

    [Test]
    public void Addition_NegatedSecondOperand_ReturnsCorrectSum()
    {
        // 10 + (−3) = 7: enter 3, negate it to −3, then add to 10.
        _calc.Enter(10).Plus().Enter(3).Negate().Equal();
        Assert.That(_calc.Result, Does.Contain("7"));
    }

    // ── Backspace ─────────────────────────────────────────────────────────────

    [Test]
    public void Backspace_RemovesLastEnteredDigit()
    {
        // Type 1-2-3, delete the 3 → display reads 12; then 12 + 3 = 15.
        _calc.Digit(1).Digit(2).Digit(3).Backspace().Plus().Digit(3).Equal();
        Assert.That(_calc.Result, Does.Contain("15"));
    }

    // ── Percent ───────────────────────────────────────────────────────────────

    [Test]
    public void Percent_InAdditionContext_AddsPercentOfFirstOperand()
    {
        // 100 + 10 % → 100 + (100 × 10%) → 100 + 10 = 110.
        _calc.Enter(100).Plus().Enter(10).Percent().Equal();
        Assert.That(_calc.Result, Does.Contain("110"));
    }
}
