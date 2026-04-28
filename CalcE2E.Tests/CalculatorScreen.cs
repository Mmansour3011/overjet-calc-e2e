using FlaUI.Core.AutomationElements;

namespace CalcE2E.Tests;

// Page Object for Windows Calculator.
// Elements are located by AutomationId — never by text or coordinates —
// so tests remain stable across locales and UI revisions.
public sealed class CalculatorScreen
{
    private readonly Window _window;

    public CalculatorScreen(Window window) => _window = window;

    private Button Btn(string id) =>
        (_window.FindFirstDescendant(cf => cf.ByAutomationId(id))
            ?? throw new InvalidOperationException($"Button '{id}' not found in Calculator window."))
        .AsButton();

    public CalculatorScreen Digit(int d)
    {
        if (d is < 0 or > 9) throw new ArgumentOutOfRangeException(nameof(d), d, "Digit must be 0-9.");
        Btn($"num{d}Button").Invoke();
        return this;
    }

    public CalculatorScreen Plus()         { Btn("plusButton").Invoke();             return this; }
    public CalculatorScreen Minus()        { Btn("minusButton").Invoke();            return this; }
    public CalculatorScreen Multiply()     { Btn("multiplyButton").Invoke();         return this; }
    public CalculatorScreen Divide()       { Btn("divideButton").Invoke();           return this; }
    public CalculatorScreen Equal()        { Btn("equalButton").Invoke();            return this; }
    public CalculatorScreen Clear()        { Btn("clearButton").Invoke();            return this; }
    public CalculatorScreen ClearEntry()   { Btn("clearEntryButton").Invoke();       return this; }
    public CalculatorScreen DecimalPoint() { Btn("decimalSeparatorButton").Invoke(); return this; }
    public CalculatorScreen Backspace()    { Btn("backSpaceButton").Invoke();        return this; }
    public CalculatorScreen Negate()       { Btn("negateButton").Invoke();           return this; }
    public CalculatorScreen Percent()      { Btn("percentButton").Invoke();          return this; }

    // The display's accessibility name takes the form "Display is <value>".
    // We strip the prefix and return the raw value string.
    public string Result
    {
        get
        {
            var el = _window.FindFirstDescendant(cf => cf.ByAutomationId("CalculatorResults"));
            var name = el?.Name ?? string.Empty;
            const string prefix = "Display is ";
            return name.StartsWith(prefix) ? name[prefix.Length..].Trim() : name.Trim();
        }
    }

    public CalculatorScreen Enter(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value,
            "Enter() only supports non-negative integers. Use Minus() before entering the absolute value.");
        foreach (var ch in value.ToString())
            Digit(ch - '0');
        return this;
    }
}
