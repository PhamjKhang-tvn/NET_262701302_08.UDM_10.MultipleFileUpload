using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApp_Client.Services
{
    public class TransferProgressCalculator
    {
        private DateTime _lastTime = DateTime.Now;
        private long _lastBytes = 0;
        public double CurrentSpeedMbps { get; private set; }
        public double ProgressPercentage { get; private set; }
        public TimeSpan ETA { get; private set; }

        public void Update(long bytesTransferred, long totalBytes)
        {
            var now = DateTime.Now;
            var timeSpan = (now - _lastTime).TotalSeconds;
            if (timeSpan >= 0.5)
            {
                long bytesDelta = bytesTransferred - _lastBytes;
                double bytesPerSecond = bytesDelta / timeSpan;
                CurrentSpeedMbps = bytesPerSecond / (1024 * 1024);
                if (totalBytes > 0) ProgressPercentage = (double)bytesTransferred / totalBytes * 100;
                long bytesRemaining = totalBytes - bytesTransferred;
                double secondsRemaining = bytesPerSecond > 0 ? bytesRemaining / bytesPerSecond : 0;
                ETA = TimeSpan.FromSeconds(secondsRemaining);
                _lastTime = now;
                _lastBytes = bytesTransferred;
            }
        }
        public void Reset() { _lastTime = DateTime.Now; _lastBytes = 0; CurrentSpeedMbps = 0; ProgressPercentage = 0; ETA = TimeSpan.Zero; }
    }

    public class FileQueueManager
    {
        private readonly ConcurrentQueue<FileTaskItem> _fileQueue = new ConcurrentQueue<FileTaskItem>();
        private CancellationTokenSource _cts;
        private bool _isRunning = false;

        public event EventHandler<FileTaskItem> OnFileStatusChanged;
        public event EventHandler<string> OnQueueCompleted;

        public void EnqueueFile(FileTaskItem item)
        {
            _fileQueue.Enqueue(item);
            item.Status = FileStatus.Pending;
            OnFileStatusChanged?.Invoke(this, item);
        }

        public void StartProcessing()
        {
            if (_isRunning) return;
            _isRunning = true;
            _cts = new CancellationTokenSource();
            Task.Run(() => ProcessQueueAsync(_cts.Token));
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            TransferProgressCalculator calculator = new TransferProgressCalculator();
            while (_fileQueue.TryDequeue(out var fileItem))
            {
                if (cancellationToken.IsCancellationRequested) break;
                try
                {
                    fileItem.Status = FileStatus.Sending;
                    OnFileStatusChanged?.Invoke(this, fileItem);
                    calculator.Reset();
                    await SendFileStreamAsync(fileItem, calculator, cancellationToken);
                    fileItem.Status = FileStatus.Completed;
                    OnFileStatusChanged?.Invoke(this, fileItem);
                }
                catch (Exception ex)
                {
                    fileItem.Status = FileStatus.Failed;
                    fileItem.ErrorMessage = ex.Message;
                    OnFileStatusChanged?.Invoke(this, fileItem);
                }
            }
            _isRunning = false;
            OnQueueCompleted?.Invoke(this, "Hoàn tất xử lý hàng đợi file.");
        }

        private async Task SendFileStreamAsync(FileTaskItem item, TransferProgressCalculator calculator, CancellationToken cancellationToken)
        {
            long simulatedTotal = item.TotalBytes > 0 ? item.TotalBytes : 1024 * 1024 * 10;
            item.TotalBytes = simulatedTotal;
            long sent = 0;
            while (sent < simulatedTotal)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int chunkSize = 64 * 1024;
                await Task.Delay(50, cancellationToken);
                sent += chunkSize;
                if (sent > simulatedTotal) sent = simulatedTotal;
                item.TransferredBytes = sent;
                calculator.Update(sent, simulatedTotal);
            }
        }

        public void Stop() { _cts?.Cancel(); _isRunning = false; }
    }
}
