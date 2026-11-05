using System;
using System.Collections.Generic;

namespace UploadClient.Models
{
    /// <summary>Thông tin GUI chuyển cho phần điều phối (TV5) / dịch vụ gửi (TV3) khi bấm "Bắt đầu gửi".</summary>
    public class StartUploadEventArgs : EventArgs
    {
        public StartUploadEventArgs(string host, int port, IReadOnlyList<UploadItem> items)
        {
            Host = host;
            Port = port;
            Items = items;
        }

        public string Host { get; }
        public int Port { get; }

        /// <summary>Các file cần gửi (đã đưa về Waiting). Phần gửi cập nhật Status/Progress/Speed/Error trên chính các đối tượng này.</summary>
        public IReadOnlyList<UploadItem> Items { get; }
    }
}
