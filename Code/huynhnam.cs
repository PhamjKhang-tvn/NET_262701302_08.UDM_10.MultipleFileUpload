using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// TV4: TcpListener, đọc/ghi file, file tạm và tên trùng
// Giao thức (thống nhất với TV3 - Client):
//   [4 byte: độ dài tên file][tên file UTF8][8 byte: kích thước file][dữ liệu file]

public class FileReceiver
{
    private TcpListener _listener;
    private bool _running;
    private readonly string _saveFolder;

    public event Action<string> OnLog;                   // ghi log ra giao diện
    public event Action<string, long, long> OnProgress;  // tên file, đã nhận, tổng

    public FileReceiver(string saveFolder)
    {
        _saveFolder = saveFolder;
        Directory.CreateDirectory(_saveFolder);
    }

    public async Task StartAsync(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _running = true;
        OnLog?.Invoke($"Server lắng nghe cổng {port}");

        while (_running)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client)); // mỗi client 1 luồng
            }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) { break; }
        }
    }

    public void Stop()
    {
        _running = false;
        _listener?.Stop();
        OnLog?.Invoke("Server đã dừng");
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        string tempPath = null;
        try
        {
            using (client)
            using (NetworkStream ns = client.GetStream())
            {
                // 1. Đọc header
                int nameLen = BitConverter.ToInt32(await ReadExactAsync(ns, 4), 0);
                string fileName = Path.GetFileName(           // chống ../ (path traversal)
                    Encoding.UTF8.GetString(await ReadExactAsync(ns, nameLen)));
                long fileSize = BitConverter.ToInt64(await ReadExactAsync(ns, 8), 0);

                OnLog?.Invoke($"Nhận file: {fileName} ({fileSize} bytes)");

                // 2. Ghi vào file tạm trước
                tempPath = Path.Combine(_saveFolder, Guid.NewGuid() + ".tmp");
                using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[8192];
                    long received = 0;
                    while (received < fileSize)
                    {
                        int toRead = (int)Math.Min(buffer.Length, fileSize - received);
                        int n = await ns.ReadAsync(buffer, 0, toRead);
                        if (n == 0) throw new IOException("Client ngắt kết nối giữa chừng");
                        await fs.WriteAsync(buffer, 0, n);
                        received += n;
                        OnProgress?.Invoke(fileName, received, fileSize);
                    }
                }

                // 3. Nhận đủ -> đổi tên file tạm thành tên thật (xử lý trùng tên)
                string finalPath = GetUniquePath(Path.Combine(_saveFolder, fileName));
                File.Move(tempPath, finalPath);
                tempPath = null;
                OnLog?.Invoke($"Đã lưu: {Path.GetFileName(finalPath)}");
            }
        }
        catch (Exception ex)
        {
            OnLog?.Invoke("Lỗi: " + ex.Message);
        }
        finally
        {
            // Lỗi giữa chừng -> xóa file tạm
            if (tempPath != null && File.Exists(tempPath)) File.Delete(tempPath);
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

    // Đọc đủ n byte (TCP có thể trả về thiếu)
    private static async Task<byte[]> ReadExactAsync(NetworkStream ns, int count)
    {
        byte[] data = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int n = await ns.ReadAsync(data, offset, count - offset);
            if (n == 0) throw new IOException("Kết nối bị đóng");
            offset += n;
        }
        return data;
    }
}

// Cách dùng trong Form Server:
//   var receiver = new FileReceiver(@"C:\ReceivedFiles");
//   receiver.OnLog += msg => Invoke(() => txtLog.AppendText(msg + Environment.NewLine));
//   await receiver.StartAsync(9000);
