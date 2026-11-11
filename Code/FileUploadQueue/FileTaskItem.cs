
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
