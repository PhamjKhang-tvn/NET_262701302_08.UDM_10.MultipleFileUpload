# TV6 - How to read and explain the code

**Primary module:** Server GUI - Automated Testing - Evidence

ServerForm.cs owns the Server UI, listener controls and event-to-UI marshaling. Tests/Program.cs is the console test harness (not xUnit), Headless/Program.cs is a test helper for separate processes, and PowerShell scripts generate demos and import real results into the teacher-format workbook.

## Read these files

- `Code/Server/ServerForm.cs`
- `Code/Headless/Program.cs`
- `Code/Tests/Program.cs`
- `Tools/Create-DemoFiles.ps1`
- `Tools/Import-TestResults.ps1`
- `02_RUN_TESTS.cmd`
- `03_CREATE_DEMO_FILES.cmd`
- `Extra/TestCases/UDM10_Test_Cases.xlsx`

## Key entry points
### `ServerForm()`
Configures Server GUI, Start/Stop buttons, session list and log controls.

### `StartServer() / StopServer()`
Calls ServerEngine; GUI shows current state.

### `UpdateRecord(ServerEvent)`
Updates sessions and logs on the Windows UI thread.

### `Tests Program.RunAll()`
Runs automated tests and measurement cases with assertions.

### `Headless Program.Run(...)`
Provides command-line serve/send to support process-level tests.

### `Import-TestResults.ps1`
Moves actual test run results into the workbook; must not fabricate passes.

## What you need from teammates
Needs ServerEngine from TV4, Theme.cs from TV2 and shared settings/models from TV1. Whole team provides manual test evidence; TV6 consolidates.

## Technical concepts to learn
- WinForms Server lifecycle, session status and persistent event logs.
- GitHub revisions and linking evidence to test case rows.
- Functional, invalid-input, disconnect, performance and stress testing.
- Automated test harness vs manual GUI/LAN test; loopback is not a LAN measurement.
- SHA-256 comparison, keeping reproducible steps and source/destination files.

## Official starting references
- https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes
- https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/
- https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.readasync?view=net-10.0
- https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/

Use Visual Studio 2026 with .NET 10 SDK. Project files and shared dependencies live in the COMMON group repository, not in this member package alone.
