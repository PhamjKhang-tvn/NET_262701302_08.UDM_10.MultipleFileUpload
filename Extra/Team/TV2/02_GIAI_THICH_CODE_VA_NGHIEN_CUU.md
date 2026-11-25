# TV2 - How to read and explain the code

**Primary module:** WinForms Client - Drag and Drop - Display

ClientForm.cs builds the WinForms screen and binds file rows to UploadJob IDs. Theme.cs contains reusable styling and UI helpers used by both GUI applications. TV2 should coordinate changes to Theme with TV6.

## Read these files

- `Code/Client/ClientForm.cs`
- `Code/UiCommon/Theme.cs`

## Key entry points
### `ClientForm()`
Creates and positions controls and hooks the UI events.

### `AddFiles(...)`
Accepts existing files, filters folders/duplicate full paths, displays file sizes.

### `Options()`
Collects and validates the endpoint and timeout values.

### `Probe()`
Checks a real TCP connection using the service provided by TV3.

### `RunUpload()`
Calls the queue from TV5 asynchronously and passes progress notifications.

### `Apply(TransferUpdate)`
Updates the corresponding file row, speed and status from updates.

### `OnDragDrop(...)`
Handles Windows drag and drop.

### `OnClosing(...)`
Requests cancellation when the form is closed during work.

## What you need from teammates
Needs Models.cs and Settings.cs from TV1, UploadClientService.cs from TV3, UploadQueue.cs from TV5, and shared Wire.cs from TV1.

## Technical concepts to learn
- WinForms controls: TextBox, NumericUpDown, DataGridView, Button.
- File drop: AllowDrop, DragEnter, DragDrop, FileDrop.
- UI thread: Progress<T> and callback marshaling; never write to a control directly from network workers.
- Keep GUI responsive with async/await.

## Official starting references
- https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes
- https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/
- https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.readasync?view=net-10.0
- https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/

Use Visual Studio 2026 with .NET 10 SDK. Project files and shared dependencies live in the COMMON group repository, not in this member package alone.
