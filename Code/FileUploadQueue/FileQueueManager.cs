using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApp_Client.Services
{
    public class FileQueueManager
    {
        private readonly ConcurrentQueue<FileTaskItem> _fileQueue = new ConcurrentQueue<FileTaskItem>();
        private readonly SemaphoreSlim _semaphore;
        
        // NGĂN CHẠY TRÙNG QUEUE: Biến cờ và khóa kiểm soát trạng thái đang chạy
        private bool _isRunning = false;
        private readonly object _lockObj = new object();

        public event EventHandler<FileTaskItem> OnFileStatusChanged;

        public FileQueueManager(int maxConcurrent = 2)
        {
            _semaphore = new SemaphoreSlim(maxConcurrent);
        }

        // Thêm file vào hàng đợi (Hỗ trợ xếp tiếp các file chưa gửi)
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChatApp_Client.Services
{
    public enum FileStatus
    {
        Pending,
        Connecting,
        Sending,
        Receiving,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    public class FileTaskItem : INotifyPropertyChanged
    {
        public string FileId { get; set; } = Guid.NewGuid().ToString();

        public string FilePath { get; set; }

        public string FileName { get; set; }

        public long TotalBytes { get; set; }

        private long _transferredBytes;

        public long TransferredBytes
        {
            get => _transferredBytes;
            set
            {
                if (_transferredBytes != value)
                {
                    _transferredBytes = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        public double ProgressPercentage
        {
            get
            {
                if (TotalBytes <= 0)
                    return 0;

                return Math.Min(100,
                    (double)TransferredBytes / TotalBytes * 100);
            }
        }

        private FileStatus _status = FileStatus.Pending;

        public FileStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _errorMessage;

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        // Reset dữ liệu khi thêm lại hoặc thử truyền lại file
        public void ResetForRetry()
        {
            TransferredBytes = 0;
            ErrorMessage = null;
            Status = FileStatus.Pending;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}

