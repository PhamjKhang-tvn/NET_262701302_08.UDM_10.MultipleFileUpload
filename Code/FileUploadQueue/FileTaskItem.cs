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
        public long TransferredBytes { get; set; }
        
        private FileStatus _status = FileStatus.Pending;
        public FileStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
