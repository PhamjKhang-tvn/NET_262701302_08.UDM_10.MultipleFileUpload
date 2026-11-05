using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;

namespace UploadClient.Models
{
    /// <summary>
    /// Trạng thái hiển thị của một file. Tên khớp với dự kiến của TV5 (UploadItem + enum trạng thái).
    /// Khi TV5/TV1 có bản chính trong Shared thì thay lớp này, giữ nguyên tên thuộc tính.
    /// </summary>
    public enum UploadStatus
    {
        Waiting,    // chờ gửi
        Uploading,  // đang gửi
        Completed,  // chỉ đặt khi Server đã xác nhận SUCCESS
        Error,      // lỗi riêng của file hoặc lỗi kết nối
        Canceled    // người dùng hủy
    }

    /// <summary>
    /// Một dòng trong bảng file. Mọi thuộc tính có thể được đặt từ luồng nền:
    /// sự kiện PropertyChanged luôn được chuyển về luồng giao diện (nơi đối tượng được tạo).
    /// </summary>
    public class UploadItem : INotifyPropertyChanged
    {
        private readonly SynchronizationContext _ctx = SynchronizationContext.Current;

        private UploadStatus _status = UploadStatus.Waiting;
        private int _progress;
        private double _speedBytesPerSec;
        private string _errorMessage = "";

        public UploadItem(string filePath, long sizeBytes, bool isDemo = false)
        {
            FilePath = filePath;
            SizeBytes = sizeBytes;
            IsDemo = isDemo;
            FileName = (isDemo ? "[DEMO] " : "") + Path.GetFileName(filePath);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // ---------- Thông tin cố định ----------
        public string FilePath { get; }
        public string FileName { get; }
        public long SizeBytes { get; }

        /// <summary>Dòng dữ liệu giả chỉ để xem bố cục; không bao giờ được gửi đi.</summary>
        public bool IsDemo { get; }

        // ---------- Thông tin thay đổi khi gửi ----------
        public UploadStatus Status
        {
            get { return _status; }
            set
            {
                if (_status == value) return;
                _status = value;
                Raise(nameof(Status), nameof(StatusText));
            }
        }

        /// <summary>0..100, tính từ số byte đã gửi / tổng byte. Không tự đổi Status thành Completed.</summary>
        public int Progress
        {
            get { return _progress; }
            set
            {
                int v = Math.Max(0, Math.Min(100, value));
                if (_progress == v) return;
                _progress = v;
                Raise(nameof(Progress), nameof(ProgressText));
            }
        }

        public double SpeedBytesPerSec
        {
            get { return _speedBytesPerSec; }
            set
            {
                double v = value < 0 || double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
                if (_speedBytesPerSec == v) return;
                _speedBytesPerSec = v;
                Raise(nameof(SpeedBytesPerSec), nameof(SpeedText));
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                string v = value ?? "";
                if (_errorMessage == v) return;
                _errorMessage = v;
                Raise(nameof(ErrorMessage));
            }
        }

        // ---------- Chuỗi hiển thị (cột của bảng gắn vào các thuộc tính này) ----------
        public string SizeText { get { return FormatSize(SizeBytes); } }

        public string StatusText
        {
            get
            {
                string s;
                switch (_status)
                {
                    case UploadStatus.Waiting: s = "Chờ gửi"; break;
                    case UploadStatus.Uploading: s = "Đang gửi"; break;
                    case UploadStatus.Completed: s = "Hoàn thành"; break;
                    case UploadStatus.Error: s = "Lỗi"; break;
                    case UploadStatus.Canceled: s = "Đã hủy"; break;
                    default: s = _status.ToString(); break;
                }
                return IsDemo ? s + " (giả lập)" : s;
            }
        }

        public string ProgressText { get { return _progress + "%"; } }

        public string SpeedText
        {
            get
            {
                if (_status != UploadStatus.Uploading || _speedBytesPerSec <= 0) return "—";
                return FormatSize((long)_speedBytesPerSec) + "/s";
            }
        }

        /// <summary>Đưa file Error/Canceled về Waiting để gửi lại. File Completed không bị đụng tới.</summary>
        public void ResetForRetry()
        {
            if (_status != UploadStatus.Error && _status != UploadStatus.Canceled) return;
            Status = UploadStatus.Waiting;
            Progress = 0;
            SpeedBytesPerSec = 0;
            ErrorMessage = "";
        }

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            string[] units = { "KB", "MB", "GB", "TB" };
            double v = bytes;
            int i = -1;
            do { v /= 1024; i++; } while (v >= 1024 && i < units.Length - 1);
            return string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", v, units[i]);
        }

        private void Raise(params string[] names)
        {
            PropertyChangedEventHandler h = PropertyChanged;
            if (h == null) return;

            Action fire = () =>
            {
                foreach (string n in names) h(this, new PropertyChangedEventArgs(n));
            };

            if (_ctx != null && SynchronizationContext.Current != _ctx)
                _ctx.Post(_ => fire(), null);   // chuyển về luồng giao diện
            else
                fire();
        }
    }
}
