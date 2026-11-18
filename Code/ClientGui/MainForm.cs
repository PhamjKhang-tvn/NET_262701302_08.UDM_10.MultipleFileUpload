using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using UploadClient.Models;

namespace UploadClient
{
    /// <summary>
    /// Form Client (TV2). Chỉ lo giao diện: nhập IP/port, chọn/kéo thả file, hiển thị bảng file,
    /// trạng thái/tiến độ/tốc độ/lỗi. Không mở socket và không gửi dữ liệu.
    /// Phần gửi (TV3) và điều phối (TV5) nối vào qua StartRequested, CancelRequested, SetUploading.
    /// </summary>
    public class MainForm : Form
    {
        // Chặng 1B: cho phép nạp dòng dữ liệu giả để xem bố cục. Đặt false từ chặng 2A.
        private const bool HienNutDuLieuGia = true;

        private readonly BindingList<UploadItem> _ds = new BindingList<UploadItem>();
        private bool _dangGui;

        private TextBox txtIP;
        private TextBox txtPort;
        private Button btnChon;
        private Button btnXoaChon;
        private Button btnXoaTatCa;
        private Button btnThemDemo;
        private Button btnGui;
        private Button btnHuy;
        private DataGridView dgvFiles;
        private DataGridViewTextBoxColumn colTrangThai;
        private ProgressBar pbTong;
        private Label lblTong;
        private Label lblThongBao;
        private ErrorProvider errorProvider;

        /// <summary>Người dùng bấm "Bắt đầu gửi" và dữ liệu nhập hợp lệ. Chạy trên luồng giao diện.</summary>
        public event EventHandler<StartUploadEventArgs> StartRequested;

        /// <summary>Người dùng bấm "Hủy" hoặc xác nhận đóng form khi đang gửi.</summary>
        public event EventHandler CancelRequested;

        public MainForm()
        {
            Text = "Upload nhiều file - Client";
            Font = new Font("Segoe UI", 10F);
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 560);
            MinimumSize = new Size(760, 460);
            AllowDrop = true;

            KhoiTaoGiaoDien();
            GanSuKien();
            CapNhatNut();
            CapNhatTong();
        }

        /// <summary>Danh sách file đang hiển thị (gắn dữ liệu với bảng).</summary>
        public BindingList<UploadItem> Items { get { return _ds; } }

        // =====================================================================
        //  Dựng giao diện bằng mã (không dùng Designer để dễ đọc và dễ ghép)
        // =====================================================================
        private void KhoiTaoGiaoDien()
        {
            errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // IP/port
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // nút thêm/xóa file
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // bảng
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // tiến độ tổng
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // Bắt đầu / Hủy
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // thông báo

            // ---- Hàng 0: IP / port ----
            txtIP = new TextBox { Width = 160, Text = "127.0.0.1" };
            txtPort = new TextBox { Width = 80, Text = "8888" };
            var hangKetNoi = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 8)
            };
            hangKetNoi.Controls.Add(TaoNhan("Server IP:", new Padding(0, 0, 6, 0)));
            hangKetNoi.Controls.Add(txtIP);
            hangKetNoi.Controls.Add(TaoNhan("Port:", new Padding(16, 0, 6, 0)));
            hangKetNoi.Controls.Add(txtPort);
            root.Controls.Add(hangKetNoi, 0, 0);

            // ---- Hàng 1: nút thêm/xóa file ----
            btnChon = TaoNut("Chọn file...");
            btnXoaChon = TaoNut("Xóa đã chọn");
            btnXoaTatCa = TaoNut("Xóa tất cả");
            btnThemDemo = TaoNut("Nạp dữ liệu giả");
            btnThemDemo.Visible = HienNutDuLieuGia;

            var goiY = TaoNhan("Kéo thả file vào bảng bên dưới hoặc bấm Chọn file.", new Padding(8, 0, 0, 0));
            goiY.ForeColor = SystemColors.GrayText;

            var hangFile = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            hangFile.Controls.Add(btnChon);
            hangFile.Controls.Add(btnXoaChon);
            hangFile.Controls.Add(btnXoaTatCa);
            hangFile.Controls.Add(btnThemDemo);
            hangFile.Controls.Add(goiY);
            root.Controls.Add(hangFile, 0, 1);

            // ---- Hàng 2: bảng file ----
            dgvFiles = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowDrop = true
            };
            colTrangThai = TaoCot("colTrangThai", "Trạng thái", nameof(UploadItem.StatusText), 16);
            dgvFiles.Columns.Add(TaoCot("colTenFile", "Tên file", nameof(UploadItem.FileName), 30));
            dgvFiles.Columns.Add(TaoCot("colKichThuoc", "Kích thước", nameof(UploadItem.SizeText), 12));
            dgvFiles.Columns.Add(colTrangThai);
            dgvFiles.Columns.Add(TaoCot("colTienDo", "Tiến độ", nameof(UploadItem.ProgressText), 10));
            dgvFiles.Columns.Add(TaoCot("colTocDo", "Tốc độ", nameof(UploadItem.SpeedText), 12));
            dgvFiles.Columns.Add(TaoCot("colLoi", "Lỗi", nameof(UploadItem.ErrorMessage), 24));
            dgvFiles.DataSource = _ds;
            root.Controls.Add(dgvFiles, 0, 2);

            // ---- Hàng 3: tiến độ tổng ----
            pbTong = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Margin = new Padding(0, 3, 12, 3)
            };
            lblTong = TaoNhan("0/0 file hoàn thành", new Padding(0));
            var hangTong = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            hangTong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            hangTong.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            hangTong.Controls.Add(pbTong, 0, 0);
            hangTong.Controls.Add(lblTong, 1, 0);
            root.Controls.Add(hangTong, 0, 3);

            // ---- Hàng 4: Bắt đầu / Hủy (xếp từ phải sang trái nên thêm Hủy trước) ----
            btnGui = TaoNut("Bắt đầu gửi");
            btnHuy = TaoNut("Hủy");
            var hangGui = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 8, 0, 0)
            };
            hangGui.Controls.Add(btnHuy);
            hangGui.Controls.Add(btnGui);
            root.Controls.Add(hangGui, 0, 4);

            // ---- Hàng 5: thông báo ----
            lblThongBao = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 26,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 8, 0, 0),
                Text = "Sẵn sàng."
            };
            root.Controls.Add(lblThongBao, 0, 5);

            Controls.Add(root);
        }

        private static Label TaoNhan(string text, Padding margin)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = margin
            };
        }

        private static Button TaoNut(string text)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 3, 10, 3),
                Margin = new Padding(0, 0, 8, 0)
            };
        }

        private static DataGridViewTextBoxColumn TaoCot(string name, string header, string property, float weight)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = property,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
        }

        // =====================================================================
        //  Sự kiện
        // =====================================================================
        private void GanSuKien()
        {
            btnChon.Click += BtnChon_Click;
            btnXoaChon.Click += BtnXoaChon_Click;
            btnXoaTatCa.Click += BtnXoaTatCa_Click;
            btnThemDemo.Click += BtnThemDemo_Click;
            btnGui.Click += BtnGui_Click;
            btnHuy.Click += BtnHuy_Click;

            // Kéo thả: nhận ở cả bảng lẫn cửa sổ để thả ở đâu cũng được
            dgvFiles.DragEnter += KeoVao;
            dgvFiles.DragDrop += ThaVao;
            DragEnter += KeoVao;
            DragDrop += ThaVao;

            dgvFiles.SelectionChanged += (s, e) => CapNhatNut();
            dgvFiles.CellFormatting += Dgv_CellFormatting;
            dgvFiles.CellToolTipTextNeeded += Dgv_CellToolTipTextNeeded;

            _ds.ListChanged += (s, e) =>
            {
                CapNhatNut();
                CapNhatTong();
            };
        }

        private void BtnChon_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog
            {
                Title = "Chọn file cần upload",
                Multiselect = true,
                CheckFileExists = true,
                Filter = "Tất cả file (*.*)|*.*"
            })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    ThemFile(dlg.FileNames);
            }
        }

        private void KeoVao(object sender, DragEventArgs e)
        {
            bool duocPhep = !_dangGui && e.Data.GetDataPresent(DataFormats.FileDrop);
            e.Effect = duocPhep ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void ThaVao(object sender, DragEventArgs e)
        {
            if (_dangGui) return;
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths != null) ThemFile(paths);
        }

        private void BtnXoaChon_Click(object sender, EventArgs e)
        {
            if (_dangGui) return;
            var chon = dgvFiles.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as UploadItem)
                .Where(x => x != null)
                .ToList();
            foreach (UploadItem it in chon) _ds.Remove(it);
            HienThongBao("Đã xóa " + chon.Count + " dòng khỏi danh sách.");
        }

        private void BtnXoaTatCa_Click(object sender, EventArgs e)
        {
            if (_dangGui) return;
            _ds.Clear();
            HienThongBao("Đã xóa toàn bộ danh sách.");
        }

        private void BtnThemDemo_Click(object sender, EventArgs e)
        {
            // Chỉ để xem bố cục. Các dòng này mang nhãn [DEMO] + "(giả lập)" và bị chặn không cho gửi.
            _ds.Add(new UploadItem(@"DEMO\bao_cao.pdf", 2516582, true)
            { Status = UploadStatus.Completed, Progress = 100 });
            _ds.Add(new UploadItem(@"DEMO\anh01.png", 870400, true)
            { Status = UploadStatus.Uploading, Progress = 42, SpeedBytesPerSec = 1250000 });
            _ds.Add(new UploadItem(@"DEMO\video.mp4", 125829120, true));
            _ds.Add(new UploadItem(@"DEMO\trung_ten.txt", 1024, true)
            { Status = UploadStatus.Error, ErrorMessage = "Server từ chối: trùng tên (mẫu)" });
            HienThongBao("Đã nạp 4 dòng dữ liệu giả để xem bố cục. Chúng không phải kết quả upload thật.");
        }

        private void BtnGui_Click(object sender, EventArgs e)
        {
            if (_dangGui) return;                       // chặn bấm hai lần tạo hai lượt gửi
            string host;
            int port;
            if (!DocCauHinh(out host, out port)) return;

            if (_ds.Any(x => x.IsDemo))
            {
                HienThongBao("Bảng đang có dòng dữ liệu giả. Bấm Xóa tất cả trước khi gửi.", true);
                return;
            }

            List<UploadItem> canGui = _ds.Where(x => x.Status != UploadStatus.Completed).ToList();
            if (canGui.Count == 0)
            {
                HienThongBao("Không có file nào cần gửi.", true);
                return;
            }

            EventHandler<StartUploadEventArgs> handler = StartRequested;
            if (handler == null)
            {
                HienThongBao("Chưa nối với dịch vụ gửi file (làm ở chặng 2A cùng TV3, TV5).", true);
                return;
            }

            foreach (UploadItem it in canGui) it.ResetForRetry();
            SetUploading(true);
            HienThongBao("Đang gửi " + canGui.Count + " file tới " + host + ":" + port + "...");

            try
            {
                handler(this, new StartUploadEventArgs(host, port, canGui));
            }
            catch (Exception ex)
            {
                SetUploading(false);
                HienThongBao("Không bắt đầu được: " + ex.Message, true);
            }
        }

        private void BtnHuy_Click(object sender, EventArgs e)
        {
            if (!_dangGui) return;
            btnHuy.Enabled = false;                    // tránh bấm nhiều lần
            HienThongBao("Đang hủy...");
            EventHandler h = CancelRequested;
            if (h != null) h(this, EventArgs.Empty);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dangGui)
            {
                DialogResult kq = MessageBox.Show(this,
                    "Đang gửi file. Hủy việc gửi và đóng ứng dụng?",
                    "Xác nhận đóng", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (kq != DialogResult.Yes)
                {
                    e.Cancel = true;
                    base.OnFormClosing(e);
                    return;
                }
                EventHandler h = CancelRequested;
                if (h != null) h(this, EventArgs.Empty);
            }
            base.OnFormClosing(e);
        }

        // =====================================================================
        //  Thêm file, kiểm tra dữ liệu nhập
        // =====================================================================
        private void ThemFile(IEnumerable<string> paths)
        {
            if (_dangGui) return;

            int them = 0, trung = 0, thuMuc = 0, khongDoc = 0, trungTen = 0;

            foreach (string path in paths)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;

                if (Directory.Exists(path)) { thuMuc++; continue; }   // chưa hỗ trợ thả thư mục
                if (!File.Exists(path)) { khongDoc++; continue; }

                string full;
                long size;
                try
                {
                    full = Path.GetFullPath(path);
                    size = new FileInfo(full).Length;                  // file 0 byte vẫn hợp lệ
                }
                catch (Exception)
                {
                    khongDoc++;
                    continue;
                }

                if (_ds.Any(x => string.Equals(x.FilePath, full, StringComparison.OrdinalIgnoreCase)))
                {
                    trung++;
                    continue;
                }

                string ten = Path.GetFileName(full);
                if (_ds.Any(x => string.Equals(Path.GetFileName(x.FilePath), ten, StringComparison.OrdinalIgnoreCase)))
                    trungTen++;

                _ds.Add(new UploadItem(full, size));
                them++;
            }

            var phan = new List<string> { "Đã thêm " + them + " file" };
            if (trung > 0) phan.Add("bỏ qua " + trung + " file đã có trong danh sách");
            if (thuMuc > 0) phan.Add("bỏ qua " + thuMuc + " thư mục (chưa hỗ trợ)");
            if (khongDoc > 0) phan.Add(khongDoc + " file không đọc được");
            if (trungTen > 0) phan.Add(trungTen + " file trùng tên với file khác, Server có thể từ chối");
            HienThongBao(string.Join("; ", phan) + ".", thuMuc > 0 || khongDoc > 0);
        }

        private bool DocCauHinh(out string host, out int port)
        {
            host = txtIP.Text.Trim();
            bool ok = true;
            errorProvider.Clear();

            bool ipHopLe = host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || ((host.Contains(".") || host.Contains(":")) && IPAddress.TryParse(host, out _));
            if (!ipHopLe)
            {
                errorProvider.SetError(txtIP, "IP không hợp lệ (ví dụ 127.0.0.1)");
                ok = false;
            }

            if (!int.TryParse(txtPort.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                errorProvider.SetError(txtPort, "Port phải là số từ 1 đến 65535");
                ok = false;
            }

            if (!ok)
            {
                HienThongBao("IP hoặc port chưa hợp lệ.", true);
                (ipHopLe ? txtPort : txtIP).Focus();
            }
            return ok;
        }

        // =====================================================================
        //  Cập nhật giao diện (các hàm public có thể gọi từ luồng nền)
        // =====================================================================

        /// <summary>Bật/tắt chế độ đang gửi: khóa các nút thao tác không hợp lệ. An toàn khi gọi từ luồng nền.</summary>
        public void SetUploading(bool dangGui)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetUploading(dangGui)));
                return;
            }
            _dangGui = dangGui;
            AllowDrop = !dangGui;
            dgvFiles.AllowDrop = !dangGui;
            CapNhatNut();
            CapNhatTong();
        }

        /// <summary>Hiện thông báo ở dòng cuối cửa sổ. An toàn khi gọi từ luồng nền.</summary>
        public void HienThongBao(string text, bool loi = false)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => HienThongBao(text, loi)));
                return;
            }
            lblThongBao.Text = text;
            lblThongBao.ForeColor = loi ? Color.Firebrick : SystemColors.ControlText;
        }

        private void CapNhatNut()
        {
            bool coFile = _ds.Count > 0;
            bool conViecGui = _ds.Any(x => x.Status != UploadStatus.Completed);

            btnChon.Enabled = !_dangGui;
            btnXoaChon.Enabled = !_dangGui && dgvFiles.SelectedRows.Count > 0;
            btnXoaTatCa.Enabled = !_dangGui && coFile;
            btnThemDemo.Enabled = !_dangGui;
            btnGui.Enabled = !_dangGui && conViecGui;
            btnHuy.Enabled = _dangGui;
            txtIP.Enabled = !_dangGui;
            txtPort.Enabled = !_dangGui;
        }

        private void CapNhatTong()
        {
            int tong = _ds.Count;
            int xong = _ds.Count(x => x.Status == UploadStatus.Completed);
            int phanTram = tong == 0 ? 0 : (int)Math.Round(_ds.Average(x => (double)x.Progress));
            pbTong.Value = Math.Max(0, Math.Min(100, phanTram));
            lblTong.Text = xong + "/" + tong + " file hoàn thành";
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colTrangThai.Index) return;
            var it = dgvFiles.Rows[e.RowIndex].DataBoundItem as UploadItem;
            if (it == null) return;

            switch (it.Status)
            {
                case UploadStatus.Completed: e.CellStyle.ForeColor = Color.SeaGreen; break;
                case UploadStatus.Error: e.CellStyle.ForeColor = Color.Firebrick; break;
                case UploadStatus.Uploading: e.CellStyle.ForeColor = Color.RoyalBlue; break;
                case UploadStatus.Canceled: e.CellStyle.ForeColor = Color.Gray; break;
            }
        }

        private void Dgv_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var it = dgvFiles.Rows[e.RowIndex].DataBoundItem as UploadItem;
            if (it == null) return;
            if (dgvFiles.Columns[e.ColumnIndex].Name == "colTenFile") e.ToolTipText = it.FilePath;
            else if (dgvFiles.Columns[e.ColumnIndex].Name == "colLoi") e.ToolTipText = it.ErrorMessage;
        }
    }
}
