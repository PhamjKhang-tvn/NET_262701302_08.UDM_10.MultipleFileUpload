using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



public class FileReceiver
{
    public const byte AckOk = 1;
    public const byte AckFail = 0;

    private const int MaxNameBytes = 255 * 4;                 
    private const long MaxFileSize = 2L * 1024 * 1024 * 1024; // 2 GB
    private const int BufferSize = 64 * 1024;
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    private TcpListener _listener;
    private CancellationTokenSource _cts;
    private readonly string _saveFolder;
    private readonly object _saveLock = new object();

    public event Action<string> OnLog;                   
    public event Action<string, long, long> OnProgress; 

    public FileReceiver(string saveFolder)
    {
        _saveFolder = saveFolder;
        Directory.CreateDirectory(_saveFolder);
    }

    public async Task StartAsync(int port)
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        OnLog?.Invoke($"Server lắng nghe cổng {port}");

        var token = _cts.Token;
        while (!token.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client, token)); // mỗi client 1 task
            }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) { break; }
            catch (OperationCanceledException) { break; }
        }
    }

    public void Stop()
    {
        _cts?.Cancel();       
        _listener?.Stop();
        OnLog?.Invoke("Server đã dừng");
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken serverToken)
    {
        string tempPath = null;
        string fileName = "?";

        using (client)
        using (NetworkStream ns = client.GetStream())
        {
            try
            {
                // 1. Đọc header (có kiểm tra giá trị)
                int nameLen = BinaryPrimitives.ReadInt32LittleEndian(
                    await ReadExactAsync(ns, 4, serverToken));
                if (nameLen <= 0 || nameLen > MaxNameBytes)
                    throw new InvalidDataException("Độ dài tên file không hợp lệ");

                fileName = Path.GetFileName(   // chống ../ (path traversal)
                    Encoding.UTF8.GetString(await ReadExactAsync(ns, nameLen, serverToken)));
                if (string.IsNullOrWhiteSpace(fileName) ||
                    fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new InvalidDataException("Tên file không hợp lệ");

                long fileSize = BinaryPrimitives.ReadInt64LittleEndian(
                    await ReadExactAsync(ns, 8, serverToken));
                if (fileSize < 0 || fileSize > MaxFileSize)
                    throw new InvalidDataException("Kích thước file không hợp lệ");

                OnLog?.Invoke($"Nhận file: {fileName} ({fileSize} bytes)");

                // 2. Ghi vào file tạm trước
                tempPath = Path.Combine(_saveFolder, Guid.NewGuid() + ".tmp");
                byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                try
                {
                    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                                                   FileShare.None, BufferSize, useAsync: true))
                    {
                        long received = 0;
                        int lastPercent = -1;
                        while (received < fileSize)
                        {
                            int toRead = (int)Math.Min(BufferSize, fileSize - received);
                            int n = await ReadWithTimeoutAsync(ns, buffer, toRead, serverToken);
                            if (n == 0) throw new IOException("Client ngắt kết nối giữa chừng");
                            await fs.WriteAsync(buffer, 0, n, serverToken);
                            received += n;

                            // chỉ báo tiến độ khi phần trăm thay đổi (tránh giật UI)
                            int percent = (int)(received * 100 / fileSize);
                            if (percent != lastPercent)
                            {
                                lastPercent = percent;
                                OnProgress?.Invoke(fileName, received, fileSize);
                            }
                        }
                        if (fileSize == 0) OnProgress?.Invoke(fileName, 0, 0);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                // 3. Nhận đủ -> đổi tên file tạm thành tên thật (xử lý trùng tên)
                string finalPath;
                lock (_saveLock) // chọn tên + di chuyển phải nguyên tử
                {
                    finalPath = GetUniquePath(Path.Combine(_saveFolder, fileName));
                    File.Move(tempPath, finalPath);
                }
                tempPath = null;
                OnLog?.Invoke($"Đã lưu: {Path.GetFileName(finalPath)}");

                // 4. Báo cho client biết đã lưu xong
                await ns.WriteAsync(new[] { AckOk }, 0, 1, serverToken);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Lỗi ({fileName}): {ex.Message}");
                try { await ns.WriteAsync(new[] { AckFail }, 0, 1); } catch { }
            }
            finally
            {
                // Lỗi giữa chừng -> xóa file tạm
                try { if (tempPath != null && File.Exists(tempPath)) File.Delete(tempPath); }
                catch { }
            }
        }
    }

    // a.txt đã có -> a (1).txt -> a (2).txt ...
    public static string GetUniquePath(string path)
    {
        if (!File.Exists(path)) return path;

        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        int i = 1;
        string newPath;
        do
        {
            newPath = Path.Combine(dir, $"{name} ({i++}){ext}");
        } while (File.Exists(newPath));
        return newPath;
    }

    // Mỗi lần đọc có timeout; quá IdleTimeout không có dữ liệu -> hủy
    private static async Task<int> ReadWithTimeoutAsync(
        NetworkStream ns, byte[] buffer, int count, CancellationToken serverToken)
    {
        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(serverToken))
        {
            cts.CancelAfter(IdleTimeout);
            try
            {
                return await ns.ReadAsync(buffer.AsMemory(0, count), cts.Token);
            }
            catch (OperationCanceledException) when (!serverToken.IsCancellationRequested)
            {
                throw new TimeoutException("Client không gửi dữ liệu quá lâu");
            }
        }
    }

    // Đọc đủ n byte (TCP có thể trả về thiếu)
    private static async Task<byte[]> ReadExactAsync(
        NetworkStream ns, int count, CancellationToken serverToken)
    {
        byte[] data = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            var chunk = new byte[count - offset];
            int n = await ReadWithTimeoutAsync(ns, chunk, chunk.Length, serverToken);
            if (n == 0) throw new IOException("Kết nối bị đóng");
            Buffer.BlockCopy(chunk, 0, data, offset, n);
            offset += n;
        }
        return data;
    }
}

// Cách dùng trong Form Server:
//   var receiver = new FileReceiver(@"C:\ReceivedFiles");
//   receiver.OnLog      += msg => Invoke(() => txtLog.AppendText(msg + Environment.NewLine));
//   receiver.OnProgress += (name, done, total) => Invoke(() =>
//       progressBar.Value = total == 0 ? 100 : (int)(done * 100 / total));
//   await receiver.StartAsync(9000);
//   ...
//   receiver.Stop();
