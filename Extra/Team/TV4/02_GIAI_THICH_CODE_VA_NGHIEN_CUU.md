# TV4 - How to read and explain the code

**Primary module:** TCP Server Engine - Receive and Store

ServerEngine.cs owns TcpListener, sessions, the concurrency gate, validation, incoming temporary files, SHA-256, unique filename reservations, final file movement, and server events. The actual GUI belongs to TV6.

## Read these files

- `Code/ServerCore/ServerEngine.cs`

## Key entry points
### `Start(ServerOptions)`
Configures listener, output folder, active-session limit and log.

### `AcceptLoop(...)`
Accepts clients and dispatches sessions without blocking the listener.

### `HandleClient(...)`
Validates requests, streams exactly the declared size, verifies END/hash and commits.

### `StopAsync()`
Stops listener and active sessions and releases resources.

### `Emit(...) and WriteLog(...)`
Sends status events and audit log messages for GUI and file logs.

## What you need from teammates
Needs Wire.cs, Models.cs and Settings.cs from TV1; protocol agreement with TV3 and ServerForm.cs from TV6.

## Technical concepts to learn
- TcpListener, accept loop and concurrent client sessions.
- Filename safety: only base names; reject traversal and restricted names.
- Use temporary .part files and a final move only after successful verification.
- SemaphoreSlim session limit and reservation of filenames to prevent concurrent writes.
- What is a partial transfer? Handling EOF/timeout without claiming success.

## Official starting references
- https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes
- https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/
- https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.readasync?view=net-10.0
- https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/

Use Visual Studio 2026 with .NET 10 SDK. Project files and shared dependencies live in the COMMON group repository, not in this member package alone.
