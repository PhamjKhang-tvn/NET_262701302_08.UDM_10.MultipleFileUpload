using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApp_Client.Services
{
    public class UploadQueue
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly string _host;
        private readonly int _port;
        private readonly int _connectTimeoutSeconds;
        private readonly int _chunkSizeKb;

        public UploadQueue(string host, int port, int maxConcurrent, int connectTimeoutSeconds, int chunkSizeKb)
        {
            _semaphore = new SemaphoreSlim(maxConcurrent);
            _host = host;
            _port = port;
            _connectTimeoutSeconds = connectTimeoutSeconds;
            _chunkSizeKb = chunkSizeKb;
        }

        public async Task EnqueueAsync(FileTaskItem fileItem, Action<long> onProgressUpdate, CancellationToken cancellationToken)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                fileItem.Status = FileStatus.Cancelled;
                fileItem.ErrorMessage = "Đã hủy bởi người dùng";
                return;
            }

            try
            {
                fileItem.Status = FileStatus.Sending;
                
                long total = fileItem.TotalBytes > 0 ? fileItem.TotalBytes : 1024 * 1024 * 5;
                fileItem.TotalBytes = total;
                long sent = 0;

                while (sent < total)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(50, cancellationToken);
                    sent += 64 * 1024;
                    if (sent > total) sent = total;
                    
                    fileItem.TransferredBytes = sent;
                    onProgressUpdate?.Invoke(sent);
                }

                fileItem.Status = FileStatus.Completed;
            }
            catch (Exception ex)
            {
                fileItem.Status = FileStatus.Failed;
                fileItem.ErrorMessage = ex.Message;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
