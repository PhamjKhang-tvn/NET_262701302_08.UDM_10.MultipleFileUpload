using System;

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

    public class FileTaskItem
    {
        public string FileId { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long TotalBytes { get; set; }
        public long TransferredBytes { get; set; }
        public FileStatus Status { get; set; } = FileStatus.Pending;
        public string ErrorMessage { get; set; }
    }
}
```[cite: 3]
