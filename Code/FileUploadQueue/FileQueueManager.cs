using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApp_Client.Services
{
    public class FileQueueManager
    {
        // 1. Hàng đợi an toàn luồng (Thread-safe Queue)
        private readonly ConcurrentQueue<FileTaskItem> _fileQueue = new ConcurrentQueue<FileTaskItem>();
        private readonly SemaphoreSlim _semaphore;
        private bool _isRunning = false;

        public event EventHandler<FileTaskItem> OnFileStatusChanged;
        public FileQueueManager(int maxConcurrent = 2)
        {
            _semaphore = new SemaphoreSlim(maxConcurrent);
        }
        public void EnqueueFile(FileTaskItem item)
        {
            _fileQueue.Enqueue(item);
            item.Status = FileStatus.Pending;
            OnFileStatusChanged?.Invoke(this, item);
        }
        {
            if (_isRunning) return;
            _isRunning = true;

            while (_fileQueue.TryDequeue(out var fileItem))
            {
              
                await _semaphore.WaitAsync();

                _ = Task.Run(async () =>
                {
                    try
                    {
                    
                        fileItem.Status = FileStatus.Sending;
                        OnFileStatusChanged?.Invoke(this, fileItem);

                        await Task.Delay(2000); 

                        fileItem.Status = FileStatus.Completed;
                        OnFileStatusChanged?.Invoke(this, fileItem);
                    }
                    catch (Exception ex)
                    {                       
                        fileItem.Status = FileStatus.Failed;
                        fileItem.ErrorMessage = ex.Message;
                        OnFileStatusChanged?.Invoke(this, fileItem);
                    }
                    finally
                    {                        
                        _semaphore.Release();
                    }
                });
            }

            _isRunning = false;
        }
    }
}
