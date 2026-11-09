using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace GuiLogServer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ServerForm());
    }
}

internal sealed class ServerForm : Form
{
    private readonly NumericUpDown _portInput = new()
    {
        Minimum = 1024,
        Maximum = 65535,
        Value = 8080,
        Width = 90
    };
    private readonly TextBox _receiveFolderInput = new() { Dock = DockStyle.Fill };
    private readonly Button _browseFolderButton = new() { Text = "Chọn thư mục...", AutoSize = true };
    private readonly Button _startButton = new() { Text = "Bật server", AutoSize = true };
    private readonly Button _stopButton = new() { Text = "Tắt server", AutoSize = true, Enabled = false };
    private readonly Button _saveButton = new() { Text = "Lưu bằng chứng", AutoSize = true };
    private readonly Label _statusLabel = new() { Text = "● Đang dừng", AutoSize = true, ForeColor = Color.Firebrick };
    private readonly Label _addressLabel = new()
    {
        Text = "Địa chỉ cho nhóm sẽ hiển thị sau khi bật server.",
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(8, 0, 0, 0)
    };
    private readonly Label _requestCountLabel = new() { Text = "Request: 0", AutoSize = true };
    private readonly ToolTip _addressToolTip = new();
    private readonly RichTextBox _logBox = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BackColor = Color.FromArgb(22, 27, 34),
        ForeColor = Color.FromArgb(220, 230, 240),
        Font = new Font("Consolas", 9.5f),
        HideSelection = false
    };
    private readonly List<string> _logLines = [];
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly FileReceiveService _fileReceiveService = new();
    private readonly System.Windows.Forms.Timer _metricsTimer = new() { Interval = 500 };

    private WebApplication? _webApplication;
    private int _requestCount;
    private bool _closing;

    public ServerForm()
    {
        Text = "GUI Log Server - Test chức năng";
        MinimumSize = new Size(850, 620);
        Size = new Size(1100, 760);
        StartPosition = FormStartPosition.CenterScreen;
        _receiveFolderInput.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "GuiLogServer",
            "Received");

        BuildLayout();
        _startButton.Click += async (_, _) => await StartServerAsync();
        _stopButton.Click += async (_, _) => await StopServerAsync();
        _browseFolderButton.Click += (_, _) => BrowseReceiveFolder();
        _saveButton.Click += (_, _) => SaveEvidence();
        _metricsTimer.Tick += (_, _) => _requestCountLabel.Text = $"Request: {Volatile.Read(ref _requestCount)}";
        _metricsTimer.Start();
        FormClosing += OnFormClosing;

        AddLog("INFO", "Ứng dụng sẵn sàng. Khi bật, server lắng nghe trên tất cả giao diện mạng của máy.");
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 6
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "GUI / LOG SERVER",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(title, 0, 0);

        var serverPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 4, 8, 4),
            WrapContents = false,
            BackColor = Color.FromArgb(239, 244, 249)
        };
        serverPanel.Controls.Add(new Label { Text = "Cổng:", AutoSize = true, Margin = new Padding(4, 8, 0, 0) });
        serverPanel.Controls.Add(_portInput);
        serverPanel.Controls.Add(_startButton);
        serverPanel.Controls.Add(_stopButton);
        serverPanel.Controls.Add(_statusLabel);
        serverPanel.Controls.Add(_requestCountLabel);
        root.Controls.Add(serverPanel, 0, 1);
        root.Controls.Add(_addressLabel, 0, 2);

        var folderPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(4, 2, 4, 2)
        };
        folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderPanel.Controls.Add(new Label
        {
            Text = "Thư mục nhận:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        folderPanel.Controls.Add(_receiveFolderInput, 1, 0);
        folderPanel.Controls.Add(_browseFolderButton, 2, 0);
        root.Controls.Add(folderPanel, 0, 3);

        var testsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6, 2, 6, 2),
            WrapContents = true
        };
        testsPanel.Controls.Add(CreateButton("Test thành công", async () => await TestSuccessAsync()));
        testsPanel.Controls.Add(CreateButton("Test dữ liệu sai", async () => await TestInvalidDataAsync()));
        testsPanel.Controls.Add(CreateButton("Test mất kết nối", async () => await TestDisconnectAsync()));
        testsPanel.Controls.Add(CreateButton("Tải thấp (10)", async () => await TestLoadAsync(10, 5)));
        testsPanel.Controls.Add(CreateButton("Tải cao (100)", async () => await TestLoadAsync(100, 25)));
        testsPanel.Controls.Add(CreateButton("Đối chiếu nguồn–đích", () =>
        {
            return CompareFilesAsync();
        }));
        testsPanel.Controls.Add(_saveButton);
        root.Controls.Add(testsPanel, 0, 4);

        var logPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        logPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        logPanel.Controls.Add(new Label
        {
            Text = "Nhật ký hoạt động (thời gian | mức độ | nội dung)",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        logPanel.Controls.Add(_logBox, 0, 1);
        root.Controls.Add(logPanel, 0, 5);

        Controls.Add(root);
    }

    private static Button CreateButton(string text, Func<Task> onClick)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 34, Margin = new Padding(4) };
        button.Click += async (_, _) => await onClick();
        return button;
    }

    private void BrowseReceiveFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Chọn thư mục dùng để lưu tệp nhận được",
            SelectedPath = Directory.Exists(_receiveFolderInput.Text)
                ? _receiveFolderInput.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _receiveFolderInput.Text = dialog.SelectedPath;
    }

    private async Task StartServerAsync()
    {
        if (_webApplication is not null)
            return;

        var port = (int)_portInput.Value;
        string receiveFolder;
        try
        {
            receiveFolder = Path.GetFullPath(_receiveFolderInput.Text.Trim());
            Directory.CreateDirectory(receiveFolder);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            AddLog("ERROR", $"Không thể dùng thư mục nhận: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Thư mục nhận không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        WebApplication? app = null;

        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = null;
                options.ListenAnyIP(port);
            });
            app = builder.Build();
            app.Run(context => HandleRequestAsync(context, receiveFolder));
            await app.StartAsync();
            _webApplication = app;
            _portInput.Enabled = false;
            _receiveFolderInput.Enabled = false;
            _browseFolderButton.Enabled = false;
            _startButton.Enabled = false;
            _stopButton.Enabled = true;
            var addresses = GetNetworkAddresses();
            string[] connectionUrls = addresses.Length == 0
                ? [$"http://localhost:{port}/"]
                : addresses.Select(address => $"http://{address}:{port}/").ToArray();
            _statusLabel.Text = "● Đang chạy trên tất cả giao diện mạng";
            _statusLabel.ForeColor = Color.DarkGreen;
            _addressLabel.Text = $"Địa chỉ kết nối (cùng mạng LAN): {string.Join("  |  ", connectionUrls)}";
            _addressToolTip.SetToolTip(_addressLabel, string.Join(Environment.NewLine, connectionUrls));
            AddLog("INFO", $"Server đã bật trên tất cả giao diện mạng, cổng {port}. Địa chỉ: {string.Join(", ", connectionUrls)}");
            AddLog("INFO", $"Thư mục nhận: {receiveFolder}");
            AddLog("INFO", $"Nhận tệp: POST /upload?filename=<tên-tệp> với nội dung request là bytes thô của tệp.");
            AddLog("WARN", "Chỉ chia sẻ trên mạng tin cậy; cho phép cổng qua firewall ở hồ sơ Private nếu Windows chặn kết nối.");
        }
        catch (IOException ex)
        {
            if (app is not null)
                await app.DisposeAsync();
            AddLog("ERROR", $"Không thể bật server trên cổng {port}: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Không thể bật server", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (InvalidOperationException ex)
        {
            if (app is not null)
                await app.DisposeAsync();
            AddLog("ERROR", $"Cấu hình server không hợp lệ: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Không thể bật server", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (SocketException ex)
        {
            if (app is not null)
                await app.DisposeAsync();
            AddLog("ERROR", $"Không thể mở cổng server {port}: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Không thể mở cổng server", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            if (app is not null)
                await app.DisposeAsync();
            AddLog("ERROR", $"Không có quyền khởi động server hoặc truy cập thư mục nhận: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task StopServerAsync()
    {
        var app = _webApplication;
        if (app is null)
            return;

        _webApplication = null;
        await app.StopAsync();
        await app.DisposeAsync();
        _portInput.Enabled = true;
        _receiveFolderInput.Enabled = true;
        _browseFolderButton.Enabled = true;
        _startButton.Enabled = true;
        _stopButton.Enabled = false;
        _statusLabel.Text = "● Đang dừng";
        _statusLabel.ForeColor = Color.Firebrick;
        _addressLabel.Text = "Địa chỉ cho nhóm sẽ hiển thị sau khi bật server.";
        _addressToolTip.SetToolTip(_addressLabel, string.Empty);
        AddLog("INFO", "Server đã dừng.");
    }

    private static string[] GetNetworkAddresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface =>
                networkInterface.OperationalStatus == OperationalStatus.Up &&
                networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
            .Select(address => address.Address)
            .Where(address =>
                address.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(address) &&
                !address.IsIPv6LinkLocal)
            .Distinct()
            .Select(address => address.ToString())
            .OrderBy(address => address, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task HandleRequestAsync(HttpContext context, string receiveFolder)
    {
        Interlocked.Increment(ref _requestCount);
        var request = context.Request;
        var path = request.Path.Value?.ToLowerInvariant() ?? "/";
        var method = request.Method;
        var statusCode = (int)HttpStatusCode.OK;
        var contentType = "text/plain; charset=utf-8";
        string responseText;

        if (path == "/health" && method == "GET")
        {
            responseText = $"{{\"status\":\"ok\",\"time\":\"{DateTimeOffset.Now:O}\"}}";
            AddLog("INFO", $"{method} /health -> 200");
        }
        else if (path == "/upload" && method == "POST")
        {
            var fileName = request.Query["filename"].ToString();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                statusCode = (int)HttpStatusCode.BadRequest;
                responseText = "Thiếu tham số filename.";
                AddLog("WARN", "POST /upload -> 400 (thiếu filename)");
            }
            else
            {
                try
                {
                    var received = await _fileReceiveService.SaveAsync(
                        request.Body,
                        receiveFolder,
                        fileName,
                        request.ContentLength,
                        context.RequestAborted);
                    statusCode = (int)HttpStatusCode.Created;
                    contentType = "application/json; charset=utf-8";
                    responseText = JsonSerializer.Serialize(new
                    {
                        received.FileName,
                        received.Size,
                        received.Sha256
                    });
                    AddLog("PASS", $"POST /upload -> 201 | {received.FileName} | {received.Size} byte | SHA-256 {received.Sha256}");
                }
                catch (InvalidReceivedFileNameException ex)
                {
                    statusCode = (int)HttpStatusCode.BadRequest;
                    responseText = ex.Message;
                    AddLog("WARN", $"POST /upload -> 400: {ex.Message}");
                }
                catch (DuplicateReceivedFileException ex)
                {
                    statusCode = (int)HttpStatusCode.Conflict;
                    responseText = ex.Message;
                    AddLog("WARN", $"POST /upload -> 409: {ex.Message}");
                }
                catch (ReceivedFileTooLargeException ex)
                {
                    statusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                    responseText = ex.Message;
                    AddLog("WARN", $"POST /upload -> 413: {ex.Message}");
                }
                catch (EndOfStreamException ex)
                {
                    statusCode = (int)HttpStatusCode.BadRequest;
                    responseText = ex.Message;
                    AddLog("WARN", $"POST /upload -> 400 (nội dung nhận chưa đủ): {ex.Message}");
                }
                catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
                {
                    AddLog("WARN", $"Client ngắt kết nối khi đang gửi tệp '{fileName}'. Tệp tạm đã được dọn.");
                    return;
                }
                catch (IOException ex)
                {
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    responseText = "Không thể nhận hoặc ghi tệp.";
                    AddLog("ERROR", $"Lỗi nhận tệp '{fileName}': {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    responseText = "Không có quyền ghi vào thư mục nhận.";
                    AddLog("ERROR", $"Không có quyền lưu tệp '{fileName}': {ex.Message}");
                }
            }
        }
        else if (path == "/echo" && method is "GET" or "POST")
        {
            string message;
            try
            {
                message = method == "GET"
                    ? request.Query["message"].ToString()
                    : await ReadRequestBodyAsync(request);
            }
            catch (IOException ex)
            {
                AddLog("WARN", $"Client ngắt kết nối khi gửi dữ liệu: {ex.Message}");
                context.Abort();
                return;
            }

            if (message.Length == 0 || message.Length > 4096)
            {
                statusCode = (int)HttpStatusCode.BadRequest;
                responseText = "message phải có từ 1 đến 4096 ký tự.";
                AddLog("WARN", $"{method} /echo -> 400 (dữ liệu rỗng hoặc quá dài)");
            }
            else
            {
                responseText = message;
                AddLog("INFO", $"{method} /echo -> 200 ({message.Length} ký tự)");
            }
        }
        else
        {
            statusCode = (int)(path is "/health" or "/echo" or "/upload"
                ? HttpStatusCode.MethodNotAllowed
                : HttpStatusCode.NotFound);
            responseText = statusCode == (int)HttpStatusCode.NotFound ? "Không tìm thấy endpoint." : "Method không được hỗ trợ.";
            AddLog("WARN", $"{method} {path} -> {statusCode}");
        }

        var response = context.Response;
        response.StatusCode = statusCode;
        response.ContentType = contentType;

        try
        {
            await response.WriteAsync(responseText, context.RequestAborted);
        }
        catch (IOException ex)
        {
            AddLog("WARN", $"Không gửi được phản hồi do kết nối bị đóng: {ex.Message}");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var buffer = new char[1024];
        var body = new StringBuilder();
        int read;
        while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            body.Append(buffer, 0, read);
            if (body.Length > 4096)
                break;
        }
        return body.ToString();
    }

    private async Task TestSuccessAsync()
    {
        await RunHttpTestAsync("Test thành công", async () =>
        {
            using var response = await _httpClient.GetAsync(GetServerUrl() + "echo?message=hello");
            var body = await response.Content.ReadAsStringAsync();
            var passed = response.StatusCode == HttpStatusCode.OK && body == "hello";
            AddLog(passed ? "PASS" : "FAIL", $"Test thành công: mong đợi 200 và 'hello', nhận {(int)response.StatusCode} và '{body}'.");
        });
    }

    private async Task TestInvalidDataAsync()
    {
        await RunHttpTestAsync("Test dữ liệu sai", async () =>
        {
            using var response = await _httpClient.GetAsync(GetServerUrl() + "echo?message=");
            var passed = response.StatusCode == HttpStatusCode.BadRequest;
            AddLog(passed ? "PASS" : "FAIL", $"Dữ liệu rỗng: mong đợi 400, nhận {(int)response.StatusCode}.");
        });
    }

    private async Task RunHttpTestAsync(string name, Func<Task> test)
    {
        if (_webApplication is null)
        {
            AddLog("FAIL", $"{name}: hãy bật server trước.");
            return;
        }

        try
        {
            await test();
        }
        catch (HttpRequestException ex)
        {
            AddLog("FAIL", $"{name}: lỗi HTTP/kết nối: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            AddLog("FAIL", $"{name}: hết thời gian chờ: {ex.Message}");
        }
    }

    private async Task TestDisconnectAsync()
    {
        if (_webApplication is null)
        {
            AddLog("FAIL", "Test mất kết nối: hãy bật server trước.");
            return;
        }

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, (int)_portInput.Value);
            var partialRequest = Encoding.ASCII.GetBytes(
                "POST /echo HTTP/1.1\r\n" +
                $"Host: 127.0.0.1:{(int)_portInput.Value}\r\n" +
                "Content-Length: 100\r\n" +
                "Connection: close\r\n\r\npartial");
            await client.GetStream().WriteAsync(partialRequest);
            client.Close();
            await Task.Delay(100);
            using var response = await _httpClient.GetAsync(GetServerUrl() + "health");
            var passed = response.IsSuccessStatusCode;
            AddLog(passed ? "PASS" : "FAIL",
                $"Test mất kết nối giữa request: client đóng socket khi POST body chưa đủ; health sau đó trả {(int)response.StatusCode}.");
        }
        catch (SocketException ex)
        {
            AddLog("FAIL", $"Test mất kết nối: không kết nối được tới server: {ex.Message}");
        }
        catch (IOException ex)
        {
            AddLog("FAIL", $"Test mất kết nối: lỗi khi gửi request dang dở: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            AddLog("FAIL", $"Test mất kết nối: server không phản hồi sau khi client ngắt: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            AddLog("FAIL", $"Test mất kết nối: hết thời gian chờ sau khi client ngắt: {ex.Message}");
        }
    }

    private async Task TestLoadAsync(int totalRequests, int maxConcurrency)
    {
        if (_webApplication is null)
        {
            AddLog("FAIL", $"Test tải {totalRequests}: hãy bật server trước.");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;
        var failedCount = 0;
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var requests = Enumerable.Range(0, totalRequests).Select(async _ =>
        {
            await semaphore.WaitAsync();
            try
            {
                using var response = await _httpClient.GetAsync(GetServerUrl() + "health");
                if (response.IsSuccessStatusCode)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failedCount);
            }
            catch (HttpRequestException)
            {
                Interlocked.Increment(ref failedCount);
            }
            catch (TaskCanceledException)
            {
                Interlocked.Increment(ref failedCount);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(requests);
        stopwatch.Stop();
        var passed = failedCount == 0;
        AddLog(passed ? "PASS" : "FAIL",
            $"Tải {(totalRequests == 10 ? "thấp" : "cao")}: {successCount}/{totalRequests} thành công, " +
            $"{failedCount} lỗi, đồng thời tối đa {maxConcurrency}, thời gian {stopwatch.Elapsed.TotalSeconds:F2}s.");
    }

    private async Task CompareFilesAsync()
    {
        using var sourceDialog = new OpenFileDialog { Title = "Chọn tệp nguồn" };
        if (sourceDialog.ShowDialog(this) != DialogResult.OK)
            return;

        using var destinationDialog = new OpenFileDialog { Title = "Chọn tệp đích nhận được" };
        if (destinationDialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var result = await FileIntegrityComparer.CompareAsync(sourceDialog.FileName, destinationDialog.FileName);
            AddLog(result.FilesMatch ? "PASS" : "FAIL",
                $"Đối chiếu nguồn–đích: {(result.FilesMatch ? "KHỚP" : "KHÔNG KHỚP")} | " +
                $"Nguồn: {Path.GetFileName(result.SourcePath)} ({result.SourceSize} byte, SHA-256 {result.SourceSha256}) | " +
                $"Đích: {Path.GetFileName(result.DestinationPath)} ({result.DestinationSize} byte, SHA-256 {result.DestinationSha256})");
        }
        catch (IOException ex)
        {
            AddLog("ERROR", $"Không đọc được tệp để đối chiếu: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            AddLog("ERROR", $"Không có quyền đọc tệp: {ex.Message}");
        }
    }

    private void SaveEvidence()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Lưu nhật ký bằng chứng",
            Filter = "Log text (*.txt)|*.txt",
            FileName = $"server-evidence-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            File.WriteAllLines(dialog.FileName, _logLines, Encoding.UTF8);
            AddLog("PASS", $"Đã lưu nhật ký bằng chứng: {dialog.FileName}");
        }
        catch (IOException ex)
        {
            AddLog("ERROR", $"Không lưu được bằng chứng: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Lỗi lưu file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            AddLog("ERROR", $"Không có quyền lưu bằng chứng: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Lỗi lưu file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string GetServerUrl() => $"http://127.0.0.1:{(int)_portInput.Value}/";

    private void AddLog(string level, string message)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AddLog(level, message)));
            return;
        }

        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} | {level,-5} | {message}";
        _logLines.Add(line);
        _logBox.AppendText(line + Environment.NewLine);
        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.ScrollToCaret();
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closing)
            return;

        _closing = true;
        e.Cancel = true;
        _metricsTimer.Stop();
        await StopServerAsync();
        _httpClient.Dispose();
        FormClosing -= OnFormClosing;
        Close();
    }
}