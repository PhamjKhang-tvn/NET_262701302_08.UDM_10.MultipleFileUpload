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
        public void EnqueueFile(FileTaskItem item)
        {
            item.ResetForRetry(); // Reset trạng thái, tốc độ, tiến độ ban đầu
            _fileQueue.Enqueue(item);
            OnFileStatusChanged?.Invoke(this, item);
        }

        // Thực thi hàng đợi (Ngăn chạy trùng lặp, xử lý bất đồng bộ và cô lập lỗi)
        public async Task StartProcessingAsync(Func<FileTaskItem, CancellationToken, Task> uploadAction, CancellationToken ct)
        {
            lock (_lockObj)
            {
                if (_isRunning) return; // Chặn không cho chạy trùng queue nếu đang chạy dở
                _isRunning = true;
            }

            try
            {
                while (_fileQueue.TryDequeue(out var fileItem))
                {
                    if (ct.IsCancellationRequested) break;

                    await _semaphore.WaitAsync(ct);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            fileItem.Status = FileStatus.Connecting;
                            OnFileStatusChanged?.Invoke(this, fileItem);

                            // Gọi hành động truyền tải thực tế
                            await uploadAction(fileItem, ct);

                            fileItem.Status = FileStatus.Completed;
                            OnFileStatusChanged?.Invoke(this, fileItem);
                        }
                        catch (Exception ex)
                        {
                            // CÔ LẬP LỖI: Lỗi ở 1 file sẽ chuyển sang Failed riêng file đó, không làm chết queue
                            fileItem.Status = FileStatus.Failed;
                            fileItem.ErrorMessage = ex.Message;
                            OnFileStatusChanged?.Invoke(this, fileItem);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }, ct);
                }
            }
            finally
            {
                lock (_lockObj)
                {
                    _isRunning = false; // Mở khóa cho phép chạy queue đợt tiếp theo
                }
            }
        }
    }
}
