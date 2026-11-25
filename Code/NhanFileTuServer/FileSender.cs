using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// TV3: Client gửi file, khớp giao thức với FileReceiver (little-endian)
//   [4 byte: độ dài tên file][tên file UTF8][8 byte: kích thước file][dữ liệu file]
//   Server trả về 1 byte: 1 = OK, 0 = lỗi

public class FileSender
{
    private const int BufferSize = 64 * 1024;

    public event Action<string, long, long> OnProgress; // tên file, đã gửi, tổng

    public async Task<bool> SendFileAsync(string host, int port, string filePath,
                                          CancellationToken ct = default)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists) throw new FileNotFoundException("Không tìm thấy file", filePath);

        byte[] nameBytes = Encoding.UTF8.GetBytes(info.Name);
        long fileSize = info.Length;

        using (var client = new TcpClient())
        {
            await client.ConnectAsync(host, port);
            using (NetworkStream ns = client.GetStream())
            {
                // Header
                byte[] header = new byte[4 + nameBytes.Length + 8];
                BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0, 4), nameBytes.Length);
                nameBytes.CopyTo(header, 4);
                BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(4 + nameBytes.Length, 8), fileSize);
                await ns.WriteAsync(header, 0, header.Length, ct);

                // Dữ liệu file
                byte[] buffer = new byte[BufferSize];
                long sent = 0;
                int lastPercent = -1;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                                               FileShare.Read, BufferSize, useAsync: true))
                {
                    int n;
                    while ((n = await fs.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        await ns.WriteAsync(buffer, 0, n, ct);
                        sent += n;

                        int percent = (int)(sent * 100 / fileSize);
                        if (percent != lastPercent)
                        {
                            lastPercent = percent;
                            OnProgress?.Invoke(info.Name, sent, fileSize);
                        }
                    }
                }
                if (fileSize == 0) OnProgress?.Invoke(info.Name, 0, 0);

                // Chờ server xác nhận
                byte[] ack = new byte[1];
                int read = await ns.ReadAsync(ack, 0, 1, ct);
                return read == 1 && ack[0] == FileReceiver.AckOk;
            }
        }
    }
}

// Cách dùng trong Form Client:
//   var sender = new FileSender();
//   sender.OnProgress += (name, done, total) => Invoke(() =>
//       progressBar.Value = total == 0 ? 100 : (int)(done * 100 / total));
//   bool ok = await sender.SendFileAsync("127.0.0.1", 9000, @"C:\test\a.txt");
//   MessageBox.Show(ok ? "Gửi thành công" : "Server báo lỗi");
