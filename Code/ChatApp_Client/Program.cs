using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApp_Client
{
    class Program
    {
        // --- CÁC HẰNG SỐ GIAO THỨC ---
        const byte MSG_HELLO = 1, MSG_HELLO_ACK = 2, MSG_FILE_INFO = 3,
                   MSG_FILE_CHUNK = 4, MSG_FILE_END = 5, MSG_ACK = 6, MSG_ERROR = 7;
        const byte MSG_READY = 8, MSG_SUCCESS = 9; 

        const int CHUNK_SIZE = 64 * 1024;      // 64KB
        const int MAX_FRAME  = 1024 * 1024;    // 1MB / frame

        static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== CLIENT GỬI FILE (TV3) - TUẦN 3 (TIMEOUT & CANCEL) ===");

            string ip = "127.0.0.1";
            int port = 8888;
            
            File.WriteAllText("file1.txt", "Day la noi dung file 1 de test tuan 3");
            File.WriteAllText("file2.txt", "Day la noi dung file 2 dai hon mot chut");

            // NOTE: Tạo Token để có thể HỦY tiến trình khi cần (Yêu cầu 3A-TV3)
            using var cts = new CancellationTokenSource();

            // NOTE: Bắt sự kiện người dùng bấm Ctrl+C để Hủy
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Ngăn không cho app tắt đột ngột
                Console.WriteLine("\n[!] Đang hủy tiến trình gửi file...");
                cts.Cancel(); // Kích hoạt Hủy
            };

            try
            {
                Console.WriteLine("\n--- Bắt đầu gửi File 1 ---");
                await SendFileAsync(ip, port, "file1.txt", percent => Console.Write($"\rTiến độ: {percent}%   "), cts.Token);
                Console.WriteLine("\n[Kết quả] File 1: SUCCESS");

                Console.WriteLine("\n--- Bắt đầu gửi File 2 ---");
                await SendFileAsync(ip, port, "file2.txt", percent => Console.Write($"\rTiến độ: {percent}%   "), cts.Token);
                Console.WriteLine("\n[Kết quả] File 2: SUCCESS");
            }
            catch (OperationCanceledException)
            {
                // NOTE: Bắt lỗi khi người dùng chủ động Hủy
                Console.WriteLine("\n[!] Tiến trình đã bị hủy bởi người dùng. Tài nguyên đã được giải phóng an toàn.");
            }
            catch (Exception ex)
            {
                // NOTE: Bắt lỗi mất mạng, timeout
                Console.WriteLine($"\n[THẤT BẠI] {ex.Message}");
            }
        }

        public static async Task SendFileAsync(string ip, int port, string filePath, Action<int> onProgress, CancellationToken token)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Không tìm thấy file: {filePath}");

            var fi = new FileInfo(filePath);

            using (var client = new TcpClient())
            {
                // NOTE: Timeout thực tế cho kết nối (30 giây)
                client.SendTimeout = 30000;
                client.ReceiveTimeout = 30000;

                // NOTE: Truyền token vào ConnectAsync để có thể Hủy khi đang kết nối
                await client.ConnectAsync(ip, port, token);
                using var stream = client.GetStream();

                // 1. HELLO
                await SendAsync(stream, MSG_HELLO, Encoding.UTF8.GetBytes("CLIENT v3"), token);
                var helloAck = await ReceiveAsync(stream, token);
                if (helloAck.type != MSG_HELLO_ACK)
                    throw new Exception("Server từ chối HELLO.");

                // 2. METADATA
                string hash = await Sha256Async(filePath);
                string info = $"{Path.GetFileName(filePath)}|{fi.Length}|{hash}";
                await SendAsync(stream, MSG_FILE_INFO, Encoding.UTF8.GetBytes(info), token);

                // 3. CHỜ READY
                var readyAck = await ReceiveAsync(stream, token);
                if (readyAck.type != MSG_READY)
                    throw new Exception($"Server từ chối nhận file. Lỗi: {Encoding.UTF8.GetString(readyAck.payload)}");

                // 4. GỬI CHUNK
                long sent = 0;
                var buf = new byte[CHUNK_SIZE];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, CHUNK_SIZE, true))
                {
                    int n;
                    while ((n = await fs.ReadAsync(buf, 0, buf.Length, token)) > 0)
                    {
                        token.ThrowIfCancellationRequested(); // NOTE: Kiểm tra xem có bấm Hủy không

                        var chunk = new byte[n];
                        Buffer.BlockCopy(buf, 0, chunk, 0, n);
                        await SendAsync(stream, MSG_FILE_CHUNK, chunk, token);
                        sent += n;
                        
                        int percent = (int)((sent * 100.0) / fi.Length);
                        onProgress?.Invoke(percent);
                    }
                }

                // 5. GỬI END & CHỜ SUCCESS
                await SendAsync(stream, MSG_FILE_END, Array.Empty<byte>(), token);
                
                // NOTE: Chờ phản hồi cuối. Nếu Server im lặng quá lâu sẽ bị timeout
                var finalAck = await ReceiveAsync(stream, token);
                
                if (finalAck.type != MSG_SUCCESS && finalAck.type != MSG_ACK)
                {
                    throw new Exception($"Server báo lỗi khi ráp file: {Encoding.UTF8.GetString(finalAck.payload)}");
                }
            } 
        }

        // --- CÁC HÀM HỖ TRỢ (Đã thêm CancellationToken) ---
        static async Task SendAsync(Stream s, byte type, byte[] payload, CancellationToken token)
        {
            if (payload.Length > MAX_FRAME) throw new InvalidOperationException("Frame quá lớn");
            var header = new byte[5];
            header[0] = type;
            WriteU32BE(header, 1, (uint)payload.Length);
            await s.WriteAsync(header, token);
            if (payload.Length > 0) await s.WriteAsync(payload, token);
            await s.FlushAsync(token);
        }

        static async Task<(byte type, byte[] payload)> ReceiveAsync(Stream s, CancellationToken token)
        {
            var header = new byte[5];
            await ReadExactlyAsync(s, header, token);
            uint len = ReadU32BE(header, 1);
            if (len > MAX_FRAME) throw new InvalidOperationException("Frame quá lớn từ server");
            var payload = new byte[len];
            if (len > 0) await ReadExactlyAsync(s, payload, token);
            return (header[0], payload);
        }

        static async Task ReadExactlyAsync(Stream s, byte[] buf, CancellationToken token)
        {
            int off = 0;
            while (off < buf.Length)
            {
                int n = await s.ReadAsync(buf.AsMemory(off), token);
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