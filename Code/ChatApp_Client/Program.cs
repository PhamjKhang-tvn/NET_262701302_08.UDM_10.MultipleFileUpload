using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp_Client
{
    class Program
    {
        const byte MSG_HELLO = 1, MSG_HELLO_ACK = 2, MSG_FILE_INFO = 3,
                   MSG_FILE_CHUNK = 4, MSG_FILE_END = 5, MSG_ACK = 6, MSG_ERROR = 7;

        const int CHUNK_SIZE = 64 * 1024;      // 64KB
        const int MAX_FRAME  = 1024 * 1024;    // 1MB / frame

        static async Task<int> Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== CLIENT GỬI FILE (TV3) - Robust & Secure ===");

            // --- validate input ---
            Console.Write("IP Server (mặc định 127.0.0.1): ");
            string ip = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

            Console.Write("Port (mặc định 8888): ");
            if (!int.TryParse(Console.ReadLine(), out int port) || port < 1 || port > 65535)
                port = 8888;

            Console.Write("Đường dẫn file (mặc định test.txt): ");
            string path = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(path)) path = "test.txt";

            Console.Write("Dùng TLS? (y/N): ");
            bool useTls = string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase);

            if (!File.Exists(path)) { Console.WriteLine($"Không thấy file {path}"); return 1; }

            try
            {
                using var client = new TcpClient
                {
                    SendTimeout    = 30_000,
                    ReceiveTimeout = 30_000
                };
                await client.ConnectAsync(ip, port);
                Console.WriteLine("Đã kết nối TCP.");

                Stream stream = client.GetStream();
                if (useTls)
                {
                    var ssl = new SslStream(stream, false, (_, _, _, err) =>
                    {
                        // DEMO: chấp nhận mọi cert. PRODUCTION: phải verify!
                        Console.WriteLine($"[TLS warning] {err}");
                        return true;
                    });
                    await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = ip,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                    });
                    stream = ssl;
                    Console.WriteLine("TLS handshake OK.");
                }

                // 1) HELLO
                await SendAsync(stream, MSG_HELLO, Encoding.UTF8.GetBytes("CLIENT v2"));
                var r = await ReceiveAsync(stream);
                if (r.type != MSG_HELLO_ACK) { Console.WriteLine($"Server từ chối (type={r.type})"); return 2; }
                Console.WriteLine($"[Server] {Encoding.UTF8.GetString(r.payload)}");

                // 2) FILE_INFO  (name|size|sha256)
                var fi = new FileInfo(path);
                string fileName = Path.GetFileName(path);
                if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                { Console.WriteLine("Tên file chứa ký tự cấm."); return 3; }

                Console.Write("Đang tính SHA-256... ");
                string hash = await Sha256Async(path);
                Console.WriteLine(hash);

                string info = $"{fileName}|{fi.Length}|{hash}";
                await SendAsync(stream, MSG_FILE_INFO, Encoding.UTF8.GetBytes(info));

                // 3) gửi chunk + progress
                long sent = 0;
                var buf = new byte[CHUNK_SIZE];
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, CHUNK_SIZE, true))
                {
                    int n;
                    while ((n = await fs.ReadAsync(buf, 0, buf.Length)) > 0)
                    {
                        var chunk = new byte[n];
                        Buffer.BlockCopy(buf, 0, chunk, 0, n);
                        await SendAsync(stream, MSG_FILE_CHUNK, chunk);
                        sent += n;
                        Console.Write($"\rĐã gửi {sent}/{fi.Length} bytes ({sent * 100.0 / fi.Length:F1}%)   ");
                    }
                }
                Console.WriteLine();

                // 4) FILE_END, chờ ACK
                await SendAsync(stream, MSG_FILE_END, Array.Empty<byte>());
                var ack = await ReceiveAsync(stream);
                Console.WriteLine(ack.type == MSG_ACK
                    ? "[Server] Nhận file OK (đã verify hash)."
                    : $"[Server] Lỗi: {Encoding.UTF8.GetString(ack.payload)}");
                return 0;
            }
            catch (Exception ex) { Console.WriteLine("Lỗi: " + ex.Message); return 99; }
        }

        // ---------- Protocol helpers ----------
        static async Task SendAsync(Stream s, byte type, byte[] payload)
        {
            if (payload.Length > MAX_FRAME) throw new InvalidOperationException("Frame quá lớn");
            var header = new byte[5];
            header[0] = type;
            WriteU32BE(header, 1, (uint)payload.Length);
            await s.WriteAsync(header);
            if (payload.Length > 0) await s.WriteAsync(payload);
            await s.FlushAsync();
        }

        static async Task<(byte type, byte[] payload)> ReceiveAsync(Stream s)
        {
            var header = new byte[5];
            await ReadExactlyAsync(s, header);
            uint len = ReadU32BE(header, 1);
            if (len > MAX_FRAME) throw new InvalidOperationException("Frame quá lớn từ server");
            var payload = new byte[len];
            if (len > 0) await ReadExactlyAsync(s, payload);
            return (header[0], payload);
        }

        static async Task ReadExactlyAsync(Stream s, byte[] buf)
        {
            int off = 0;
            while (off < buf.Length)
            {
                int n = await s.ReadAsync(buf.AsMemory(off));
                if (n == 0) throw new IOException("Kết nối đóng giữa chừng");
                off += n;
            }
        }

        static void WriteU32BE(byte[] b, int o, uint v)
        {
            b[o] = (byte)(v >> 24); b[o+1] = (byte)(v >> 16);
            b[o+2] = (byte)(v >> 8); b[o+3] = (byte)v;
        }

        static uint ReadU32BE(byte[] b, int o) =>
            ((uint)b[o] << 24) | ((uint)b[o+1] << 16) | ((uint)b[o+2] << 8) | b[o+3];

        static async Task<string> Sha256Async(string path)
        {
            using var sha = SHA256.Create();
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
            return Convert.ToHexString(await sha.ComputeHashAsync(fs));
        }
    }
}