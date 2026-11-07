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
        // --- CÁC HẰNG SỐ GIAO THỨC (PROTOCOL CONSTANTS) ---
        // NOTE: Đây là quy ước giữa Client và Server. 
        // Nhờ các con số này, Server biết gói tin tiếp theo là gì mà xử lý.
        const byte MSG_HELLO = 1, MSG_HELLO_ACK = 2, MSG_FILE_INFO = 3,
                   MSG_FILE_CHUNK = 4, MSG_FILE_END = 5, MSG_ACK = 6, MSG_ERROR = 7;
        
        // NOTE: Bổ sung cho Tuần 2. Khi Server nhận được thông tin file, nó trả READY (8).
        // Khi Server ráp xong file và kiểm tra Hash chuẩn, nó trả SUCCESS (9).
        const byte MSG_READY = 8, MSG_SUCCESS = 9; 

        // NOTE: Kích thước mỗi khối khi chia nhỏ file (64KB). 
        // Giúp gửi file lớn mà không bị tràn RAM.
        const int CHUNK_SIZE = 64 * 1024;      
        
        // NOTE: Giới hạn kích thước tối đa của 1 gói tin (1MB) để chống lỗi bộ nhớ.
        const int MAX_FRAME  = 1024 * 1024;    

        static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== CLIENT GỬI FILE (TV3) - TUẦN 2 ===");

            string ip = "127.0.0.1";
            int port = 8888;
            
            // NOTE: Tạo 2 file test để giả lập tính năng Hàng đợi (Queue) của TV5.
            File.WriteAllText("file1.txt", "Day la noi dung file 1");
            File.WriteAllText("file2.txt", "Day la noi dung file 2 dai hon mot chut de test chia khoi");

            try
            {
                Console.WriteLine("\n--- Bắt đầu gửi File 1 ---");
                // NOTE: Truyền một hàm Lambda (percent => ...) để hứng tiến độ gửi.
                await SendFileAsync(ip, port, "file1.txt", percent => Console.Write($"\rTiến độ: {percent}%   "));
                Console.WriteLine("\n[Kết quả] File 1: SUCCESS");

                Console.WriteLine("\n--- Bắt đầu gửi File 2 ---");
                await SendFileAsync(ip, port, "file2.txt", percent => Console.Write($"\rTiến độ: {percent}%   "));
                Console.WriteLine("\n[Kết quả] File 2: SUCCESS");
            }
            catch (Exception ex)
            {
                // NOTE: Nếu 1 file bị lỗi, nó sẽ nhảy vào đây. 
                // Nhờ vậy, lỗi của file này không làm sập toàn bộ chương trình.
                Console.WriteLine($"\n[THẤT BẠI] {ex.Message}");
            }
        }

        /// <summary>
        /// NOTE: Hàm gửi file độc lập. 
        /// Yêu cầu 2B-TV3: Mỗi lần gọi hàm này sẽ tạo 1 kết nối mới tinh, và đóng lại khi xong.
        /// </summary>
        public static async Task SendFileAsync(string ip, int port, string filePath, Action<int> onProgress)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Không tìm thấy file: {filePath}");

            var fi = new FileInfo(filePath);
            string fileName = Path.GetFileName(filePath);
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new Exception("Tên file chứa ký tự cấm.");

            // NOTE: KHỐI USING (QUAN TRỌNG): Tự động gọi hàm Dispose() để đóng Socket khi xong việc hoặc khi có lỗi.
            // Đáp ứng yêu cầu "đóng stream/socket sau mỗi lượt".
            using (var client = new TcpClient())
            {
                client.SendTimeout = 30_000;    // NOTE: Timeout gửi (30 giây)
                client.ReceiveTimeout = 30_000; // NOTE: Timeout nhận (30 giây)

                await client.ConnectAsync(ip, port);
                using var stream = client.GetStream();

                // 1. HELLO
                await SendAsync(stream, MSG_HELLO, Encoding.UTF8.GetBytes("CLIENT v2"));
                var helloAck = await ReceiveAsync(stream);
                if (helloAck.type != MSG_HELLO_ACK)
                    throw new Exception("Server từ chối HELLO.");

                // 2. GỬI METADATA (Tên file | Kích thước | Mã Hash SHA-256)
                string hash = await Sha256Async(filePath);
                string info = $"{fileName}|{fi.Length}|{hash}";
                await SendAsync(stream, MSG_FILE_INFO, Encoding.UTF8.GetBytes(info));

                // 3. CHỜ SERVER BÁO READY
                // NOTE: Đây là điểm mấu chốt của Tuần 2. 
                // Client không gửi ồ ạt. Nó chờ Server xác nhận "Tao sẵn sàng nhận rồi" thì mới gửi.
                var readyAck = await ReceiveAsync(stream);
                if (readyAck.type != MSG_READY)
                    throw new Exception($"Server từ chối nhận file. Lỗi: {Encoding.UTF8.GetString(readyAck.payload)}");

                // 4. GỬI TỪNG KHỐI (CHUNKING)
                long sent = 0;
                var buf = new byte[CHUNK_SIZE];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, CHUNK_SIZE, true))
                {
                    int n;
                    while ((n = await fs.ReadAsync(buf, 0, buf.Length)) > 0)
                    {
                        var chunk = new byte[n];
                        Buffer.BlockCopy(buf, 0, chunk, 0, n);
                        await SendAsync(stream, MSG_FILE_CHUNK, chunk);
                        sent += n;
                        
                        // NOTE: Tính toán % và gọi hàm callback để báo lên GUI.
                        int percent = (int)((sent * 100.0) / fi.Length);
                        onProgress?.Invoke(percent);
                    }
                }

                // 5. GỬI TÍN HIỆU KẾT THÚC VÀ CHỜ SUCCESS
                // NOTE: Gửi xong 100% byte chưa chắc là thành công. 
                // Phải chờ Server ráp đủ file, check Hash đúng, trả về SUCCESS thì mới coi là xong.
                await SendAsync(stream, MSG_FILE_END, Array.Empty<byte>());
                var finalAck = await ReceiveAsync(stream);
                
                if (finalAck.type == MSG_SUCCESS || finalAck.type == MSG_ACK)
                {
                    // Thành công
                }
                else
                {
                    throw new Exception($"Server báo lỗi khi ráp file: {Encoding.UTF8.GetString(finalAck.payload)}");
                }
            } 
        }

        // ---------- Protocol helpers ----------

        // NOTE: Hàm đóng gói dữ liệu (Framing). 
        // Tạo Header 5 byte (1 byte loại tin, 4 byte độ dài) để chống dính gói tin (Sticky Packet).
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

        // NOTE: Hàm bóc tách dữ liệu. Đọc 5 byte Header trước, sau đó đọc đúng số byte Payload.
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

        // NOTE: Đảm bảo đọc đủ số byte cần thiết. Nếu mạng lag, nó sẽ chờ đọc tiếp.
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

        // NOTE: Chuyển đổi số nguyên sang dạng 4 byte Big-Endian để gửi qua mạng.
        static void WriteU32BE(byte[] b, int o, uint v)
        {
            b[o] = (byte)(v >> 24); b[o+1] = (byte)(v >> 16);
            b[o+2] = (byte)(v >> 8); b[o+3] = (byte)v;
        }

        static uint ReadU32BE(byte[] b, int o) =>
            ((uint)b[o] << 24) | ((uint)b[o+1] << 16) | ((uint)b[o+2] << 8) | b[o+3];

        // NOTE: Tính mã băm SHA-256 của file để đảm bảo toàn vẹn dữ liệu.
        static async Task<string> Sha256Async(string path)
        {
            using var sha = SHA256.Create();
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
            return Convert.ToHexString(await sha.ComputeHashAsync(fs));
        }
    }
}