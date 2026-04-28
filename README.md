# overjet-calc-e2e

Automated E2E test suite for the Windows Calculator app, built with **FlaUI** (UIA3) and **NUnit 4**.

---

## Environment

| Item | Value |
|---|---|
| OS | Windows 10 Pro, Build **19042** (20H2) |
| Calculator | Windows built-in Calculator (UWP) |
| .NET SDK | 10.0.202 |
| FlaUI | 5.0.0 |
| NUnit | 4.3.2 |

---

## Test Coverage

| Area | Test Count | Notes |
|---|---|---|
| Addition | 2 | Includes 0+0 edge case |
| Subtraction | 3 | Includes negative result |
| Multiplication | 3 | Includes ×0 edge case |
| Integer division | 2 | Includes zero-dividend (0÷5) edge case |
| Non-integer division | 1 | **1 ÷ 3 = 0.3333…** (handles locale decimal separator) |
| Division by zero | 1 | Expects "Cannot divide by zero" message |
| 0 ÷ 0 (undefined) | 1 | Expects "undefined" message (distinct from n÷0) |
| Clear (C) | 1 | Resets display to "0" after a calculation |
| Clear Entry (CE) | 1 | Wipes current entry, preserves first operand |
| Chained operations | 3 | Result carries forward; operator-change; repeated = |
| Decimal input | 3 | 1.5+2.5; 0.1+0.9; 0.5×6 |
| Negate (±) | 2 | Sign flip; negated operand in addition |
| Backspace | 1 | Removes last digit, then continues calculation |
| Percent (%) | 1 | 100 + 10% = 110 |
| **Total** | **25** | |

---

## Prerequisites

1. **Windows 10 or 11** — FlaUI uses the Windows Accessibility API (UIA3), which is Windows-only.
2. **.NET 10 SDK** — [Download from microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0)
3. **Windows Calculator** — pre-installed on all modern Windows systems; no additional install needed.
4. **Run as standard user** — no administrator privileges required.

---

## How to Run

```bash
# Clone the repo
git clone <repo-url>
cd overjet-calc-e2e

# Restore packages and run all tests
dotnet test

# With verbose output (shows each test name and result)
dotnet test --logger "console;verbosity=detailed"
```

Expected output (all passing):

```
Passed!  - Failed: 0, Passed: 25, Skipped: 0, Total: 25
```

> **Note:** The Calculator window will open and close rapidly for each of the 28 tests. This is normal — do not interact with the Calculator while tests are running.

---

## How It Works

The project uses a **Page Object Model** pattern:

- **`CalculatorScreen.cs`** — wraps all FlaUI interactions. Finds buttons by their Windows AccessibilityId (e.g. `"plusButton"`) rather than by text or screen coordinates, making tests stable across different Windows locales and UI scale settings.
- **`CalculatorTests.cs`** — NUnit test class. Each test launches a fresh Calculator in `[SetUp]`, interacts via the Page Object's fluent API, asserts the result, then closes the Calculator in `[TearDown]`.

---

## Known Limitations

- **Windows only** — FlaUI targets the Windows Accessibility API; this suite cannot run on macOS or Linux.
- **One Calculator instance at a time** — the suite kills any running Calculator before each test. Do not run this alongside manual Calculator use.
- **Non-integer division locale** — `1 ÷ 3` is asserted as starting with `"0.3333"` or `"0,3333"` to handle both `.` (en-US) and `,` (EU locales) decimal separators.
- **UWP launch delay** — the suite polls up to 5 seconds for `CalculatorApp.exe` to appear after `calc.exe` is started. On very slow machines this timeout may need increasing.
- **No parallel test execution** — tests are single-threaded because they all share the same physical screen and Calculator process.
