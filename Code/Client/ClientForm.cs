using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Udm10.Shared;
using Udm10.UI;

namespace Udm10.Client
{
    // TV2: GUI only. TV3 owns UploadClientService; TV5 owns UploadQueue.
    public sealed class ClientForm : Form
    {
        private sealed class RowInfo
        {
            internal UploadJob Job;
            internal DataGridViewRow Row;
            internal string State="Waiting";
            internal long Total;
        }
        private readonly List<RowInfo> rows=new List<RowInfo>();
        private readonly Dictionary<string,RowInfo> index=new Dictionary<string,RowInfo>();
        private TextBox host,log;
        private NumericUpDown port,timeout;
        private DataGridView grid;
        private Label connection,summary,drop;
        private Button add,remove,upload,cancel,retry,probe;
        private bool running,checking,closeAfter;
        private CancellationTokenSource operation;
        private AppSettings settings;

        public ClientForm()
        {
            settings=AppSettings.Load("client");
            Theme.Form(this,"UDM10 | Multi-file Upload — Client");
            var layout=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,RowCount=8,Padding=new Padding(14) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            int[] fixedHeights={86,58,34,65,58};
            foreach(int h in fixedHeights) layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,35));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,105));
            Controls.Add(layout);
            layout.Controls.Add(Theme.Header("MULTI-FILE UPLOAD","UDM_10  /  C# • WinForms • TCP  /  Giới hạn: 1 file đang tải trên mỗi Client"),0,0);
            var endpoint=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(3,8,0,0) };
            host=new TextBox { Text=settings.Host??"127.0.0.1",Width=150,Margin=new Padding(4,6,12,4) };
            port=Theme.Number(settings.Port,1,65535); timeout=Theme.Number(settings.Timeout,1,600);
            probe=Theme.Button("Kiểm tra kết nối");
            endpoint.Controls.AddRange(new Control[] { Theme.Label("Server IPv4"),host,Theme.Label("Port"),port,Theme.Label("Timeout (s)"),timeout,probe });
            layout.Controls.Add(endpoint,0,1);
            connection=Theme.Label("Chưa kiểm tra Server. Mỗi file dùng một kết nối TCP riêng.");
            layout.Controls.Add(connection,0,2);
            drop=new Label { Text="KÉO THẢ FILE VÀO ĐÂY  •  hoặc chọn bằng nút Thêm file",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(226,242,240),ForeColor=Theme.Teal,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Segoe UI Semibold",12F),Margin=new Padding(0,3,0,5) };
            layout.Controls.Add(drop,0,3);
            var tools=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false };
            add=Theme.Button("Thêm file"); remove=Theme.Button("Bỏ file đã chọn"); upload=Theme.Button("Tải các file chờ",true); cancel=Theme.Button("Dừng lượt tải"); retry=Theme.Button("Đưa file lỗi về hàng chờ");
            tools.Controls.AddRange(new Control[]{add,remove,upload,cancel,retry}); layout.Controls.Add(tools,0,4);
            grid=Theme.Grid();
            Theme.Column(grid,"Name","Tên file",190,true); Theme.Column(grid,"Size","Kích thước",105);
            Theme.Column(grid,"Progress","Tiến trình",115); Theme.Column(grid,"Speed","Tốc độ TB",100);
            Theme.Column(grid,"State","Trạng thái",100); Theme.Column(grid,"Detail","Thông tin / lỗi",220,true);
            layout.Controls.Add(grid,0,5);
            summary=Theme.Label(""); layout.Controls.Add(summary,0,6); log=Theme.LogBox(); layout.Controls.Add(log,0,7);
            foreach(Control target in new Control[] { this,drop,grid })
            { target.AllowDrop=true; target.DragEnter+=OnDragEnter; target.DragDrop+=OnDragDrop; }
            grid.CellPainting+=PaintProgress;
            add.Click+=delegate { using(var d=new OpenFileDialog { Multiselect=true,CheckFileExists=true,Title="Chọn một hoặc nhiều file" }) if(d.ShowDialog(this)==DialogResult.OK) AddFiles(d.FileNames); };
            remove.Click+=delegate
            {
                var selected=grid.SelectedRows.Cast<DataGridViewRow>().Select(r => (RowInfo)r.Tag).ToList();
                foreach(var row in selected) { rows.Remove(row); index.Remove(row.Job.Id); grid.Rows.Remove(row.Row); }
                RefreshState();
            };
            retry.Click+=delegate
            {
                if(rows.Any(r => r.State=="Error" && Convert.ToString(r.Row.Cells["Detail"].Value).Contains("UNKNOWN_RESULT")))
                    MessageBox.Show(this,"Có file chưa rõ kết quả cuối. Kiểm tra file và log trên Server trước khi tải lại. File trùng tên sẽ bị từ chối, không ghi đè.","Kiểm tra Server",MessageBoxButtons.OK,MessageBoxIcon.Information);
                foreach(var r in rows.Where(r => r.State=="Error")) SetWaiting(r);
                RefreshState();
            };
            upload.Click+=async delegate { await RunUpload(); };
            cancel.Click+=delegate { if(operation!=null) operation.Cancel(); };
            probe.Click+=async delegate { await Probe(); };
            FormClosing+=OnClosing;
            Theme.Append(log,"Sẵn sàng. Chỉ dùng trong LAN tin cậy; dữ liệu truyền bằng TCP chưa mã hóa TLS.");
            RefreshState();
        }
        private ClientOptions Options()
        { var o=new ClientOptions { Host=host.Text.Trim(),Port=(int)port.Value,TimeoutSeconds=(int)timeout.Value }; o.Validate(); return o; }
        private void SaveSettings()
        {
            settings.Host=host.Text.Trim(); settings.Port=(int)port.Value; settings.Timeout=(int)timeout.Value;
            try { settings.Save("client"); } catch(Exception ex) { Theme.Append(log,"Không lưu được cấu hình: "+ex.Message); }
        }
        private void OnDragEnter(object sender,DragEventArgs e)
        { e.Effect=!running && !checking && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; }
        private void OnDragDrop(object sender,DragEventArgs e)
        { if(!running && !checking && e.Data.GetDataPresent(DataFormats.FileDrop)) AddFiles((string[])e.Data.GetData(DataFormats.FileDrop)); }
        private void AddFiles(IEnumerable<string> paths)
        {
            int skipped=0;
            foreach(string path in paths)
            {
                if(rows.Count>=1000) { skipped++; continue; }
                try
                {
                    string full=Path.GetFullPath(path);
                    if(!File.Exists(full) || rows.Any(r => string.Equals(r.Job.Path,full,StringComparison.OrdinalIgnoreCase))) { skipped++; continue; }
                    long size=new FileInfo(full).Length;
                    var info=new RowInfo { Job=new UploadJob(full),Total=size };
                    int n=grid.Rows.Add(Path.GetFileName(full),FileRules.Bytes(size),0,"—","Waiting","Đang chờ");
                    info.Row=grid.Rows[n]; info.Row.Tag=info; info.Row.Cells["Name"].ToolTipText=full;
                    rows.Add(info); index[info.Job.Id]=info;
                }
                catch(Exception ex) { skipped++; Theme.Append(log,"Không thêm được file: "+ex.Message); }
            }
            if(skipped>0) Theme.Append(log,"Đã bỏ qua "+skipped+" mục (thư mục, file trùng đường dẫn, không đọc được hoặc vượt 1.000 mục).");
            RefreshState();
        }
        private void SetWaiting(RowInfo r)
        { r.State="Waiting"; r.Row.Cells["State"].Value=r.State; r.Row.Cells["Progress"].Value=0; r.Row.Cells["Speed"].Value="—"; r.Row.Cells["Detail"].Value="Đang chờ"; }
        private void Apply(TransferUpdate u)
        {
            RowInfo r; if(!index.TryGetValue(u.JobId,out r)) return;
            string before=r.State; r.State=u.State;
            r.Row.Cells["State"].Value=u.State;
            if(u.Total>0 || u.State=="Completed") r.Total=u.Total;
            if(u.State!="Error") r.Row.Cells["Progress"].Value=u.Percent;
            r.Row.Cells["Speed"].Value=u.BytesPerSecond>0 ? FileRules.Bytes((long)u.BytesPerSecond)+"/s" : "—";
            r.Row.Cells["Detail"].Value=u.Detail; r.Row.Cells["Detail"].ToolTipText=u.Detail;
            r.Row.Cells["State"].Style.ForeColor=u.State=="Error"?Color.Firebrick:u.State=="Completed"?Theme.Teal:Theme.Ink;
            if(before!=u.State || u.State=="Error") Theme.Append(log,DateTime.Now.ToString("HH:mm:ss")+" | "+Path.GetFileName(r.Job.Path)+" | "+u.State+" | "+u.Detail);
            RefreshState();
        }
        private async Task Probe()
        {
            ClientOptions o; try { o=Options(); } catch(Exception ex) { MessageBox.Show(this,ex.Message,"Cấu hình",MessageBoxButtons.OK,MessageBoxIcon.Warning); return; }
            checking=true; operation=new CancellationTokenSource(); RefreshState(); SaveSettings(); connection.Text="Đang kiểm tra Server...";
            try
            {
                await new UploadClientService().ProbeAsync(o,operation.Token);
                connection.Text="Server phản hồi lúc "+DateTime.Now.ToString("HH:mm:ss")+". Kết nối kiểm tra đã đóng."; connection.ForeColor=Theme.Teal;
            }
            catch(Exception ex) { connection.Text="Kiểm tra thất bại: "+ex.Message; connection.ForeColor=Color.Firebrick; Theme.Append(log,connection.Text); }
            finally { operation.Dispose(); operation=null; checking=false; RefreshState(); if(closeAfter) Close(); }
        }
        private async Task RunUpload()
        {
            if(running || checking) return;
            ClientOptions o; try { o=Options(); } catch(Exception ex) { MessageBox.Show(this,ex.Message,"Cấu hình",MessageBoxButtons.OK,MessageBoxIcon.Warning); return; }
            var jobs=rows.Where(r => r.State=="Waiting").Select(r => r.Job).ToList(); if(jobs.Count==0) return;
            running=true; operation=new CancellationTokenSource(); SaveSettings(); RefreshState();
            connection.Text="Đang xử lý hàng đợi..."; connection.ForeColor=Theme.Teal;
            // Progress<T> marshals notifications back to the captured Windows Forms UI context.
            var progress=new Progress<TransferUpdate>(Apply);
            try
            {
                var q=new UploadQueue();
                var result=await q.RunAsync(jobs,o,u => ((IProgress<TransferUpdate>)progress).Report(u),operation.Token);
                connection.Text=result.ConnectionLost ? "Mất kết nối / chưa rõ xác nhận cuối. File chưa gửi vẫn ở Waiting." : "Lượt tải đã kết thúc; kết nối từng file đã đóng.";
                Theme.Append(log,"Kết quả lượt: "+result.Completed+" hoàn tất; "+result.Errors+" lỗi; "+result.Waiting+" còn chờ.");
            }
            catch(Exception ex) { Theme.Append(log,"Lỗi lượt tải: "+ex.Message); connection.Text="Lượt tải lỗi — xem log."; }
            finally { operation.Dispose(); operation=null; running=false; RefreshState(); if(closeAfter) Close(); }
        }
        private void RefreshState()
        {
            bool busy=running||checking;
            add.Enabled=!busy; remove.Enabled=!busy; retry.Enabled=!busy && rows.Any(r => r.State=="Error");
            upload.Enabled=!busy && rows.Any(r => r.State=="Waiting"); cancel.Enabled=busy; probe.Enabled=!busy;
            host.Enabled=port.Enabled=timeout.Enabled=!busy;
            summary.Text=string.Format("Tổng: {0}   |   Chờ: {1}   |   Đang tải: {2}   |   Hoàn tất: {3}   |   Lỗi: {4}",rows.Count,rows.Count(r=>r.State=="Waiting"),rows.Count(r=>r.State=="Uploading"),rows.Count(r=>r.State=="Completed"),rows.Count(r=>r.State=="Error"));
        }
        private void OnClosing(object sender,FormClosingEventArgs e)
        {
            if(running||checking)
            {
                e.Cancel=true;
                if(!closeAfter && MessageBox.Show(this,"Dừng tác vụ hiện tại và đóng Client? File đang tải không được tiếp tục từ byte cũ.","Đóng Client",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)
                { closeAfter=true; if(operation!=null) operation.Cancel(); }
            }
            else SaveSettings();
        }
        private void PaintProgress(object sender,DataGridViewCellPaintingEventArgs e)
        {
            if(e.RowIndex<0 || e.ColumnIndex!=grid.Columns["Progress"].Index) return;
            e.PaintBackground(e.ClipBounds,true);
            int value=0; if(e.Value!=null) int.TryParse(Convert.ToString(e.Value),out value);
            Rectangle rect=new Rectangle(e.CellBounds.X+8,e.CellBounds.Y+9,e.CellBounds.Width-16,e.CellBounds.Height-18);
            using(var bg=new SolidBrush(Color.FromArgb(225,233,237))) e.Graphics.FillRectangle(bg,rect);
            if(value>0) using(var fg=new SolidBrush(Color.FromArgb(83,183,163))) e.Graphics.FillRectangle(fg,new Rectangle(rect.X,rect.Y,(int)(rect.Width*Math.Min(100,value)/100d),rect.Height));
            TextRenderer.DrawText(e.Graphics,value+"%",grid.Font,e.CellBounds,Theme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            e.Handled=true;
        }
    }
}