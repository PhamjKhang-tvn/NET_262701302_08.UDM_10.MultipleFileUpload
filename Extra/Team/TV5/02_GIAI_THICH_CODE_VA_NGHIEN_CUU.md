# TV5 - How to read and explain the code

**Primary module:** Queue - Per-file State - Progress

UploadQueue.cs runs one file per Client at a time. It separates a local file error (continue with next item) from a server-connection outage (keep remaining items Waiting). UI state display is coordinated with TV2, and UploadJob/TransferUpdate models are part of TV1 shared files.

## Read these files

- `Code/Shared/UploadQueue.cs`

## Key entry points
### `RunAsync(...)`
Processes selected UploadJob items in sequence and returns totals.

### `Interlocked.Exchange(...)`
Prevents two queue executions from running simultaneously.

### `catch(UploadException)`
Marks the failed file and decides whether to continue or stop.

### `catch(OperationCanceledException)`
Stops the running batch without marking every Waiting item as failed.

### `result.Waiting/Completed/Errors`
Tracks per-batch outcomes without changing actual test results.

## What you need from teammates
Needs Models.cs (UploadJob, TransferUpdate) from TV1, UploadClientService.cs from TV3, and GUI state mapping from TV2.

## Technical concepts to learn
- Queue ordering and the distinction between per-Client and server-wide concurrency.
- States Waiting, Uploading, Completed, Error; final ACK before Completed.
- Progress as sentBytes/totalBytes, safe handling of zero bytes, speed as bytes/elapsedSeconds.
- Error isolation: local failure vs connection unavailable; no blind retry or byte-level resume.

## Official starting references
- https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes
- https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/
- https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.readasync?view=net-10.0
- https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/

Use Visual Studio 2026 with .NET 10 SDK. Project files and shared dependencies live in the COMMON group repository, not in this member package alone.
