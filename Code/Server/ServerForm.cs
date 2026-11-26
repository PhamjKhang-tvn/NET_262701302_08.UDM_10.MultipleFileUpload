using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Udm10.Shared;
using Udm10.ServerCore;
using Udm10.UI;

namespace Udm10.Server
{
    // TV6: Server GUI + log presentation. TV4: ServerEngine.
    public sealed class ServerForm : Form
    {
        private ComboBox address;
        private NumericUpDown port,slots,timeout,maxMiB;
        private TextBox folder,log;
        private Button browse,start,stop,open,openLog;
        private DataGridView grid;
        private Label status,policy;
        private readonly Dictionary<string,DataGridViewRow> records=new Dictionary<string,DataGridViewRow>();
        private ServerEngine engine;
        private AppSettings settings;
        private bool stopping,closeAfter;
        private readonly Timer heartbeat=new Timer();

        public ServerForm()
        {
            settings=AppSettings.Load("server");
            Theme.Form(this,"UDM10 | Multi-file Upload — Server");
            var root=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,RowCount=8,Padding=new Padding(14) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            foreach(int h in new int[]{86,55,55,55,55,35}) root.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
            root.RowStyles.Add(new RowStyle(SizeType.Percent,55)); root.RowStyles.Add(new RowStyle(SizeType.Percent,45));
            Controls.Add(root);
            root.Controls.Add(Theme.Header("UPLOAD SERVER","UDM_10  /  Nhận file qua TCP  /  Xác minh SHA-256 • Không ghi đè • Lưu log"),0,0);
            var line1=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(2,6,0,0) };
            address=new ComboBox { Width=150,DropDownStyle=ComboBoxStyle.DropDown,Margin=new Padding(4,6,12,4) };
            address.Items.AddRange(new object[]{"127.0.0.1","0.0.0.0"});
            try { foreach(var ip in Dns.GetHostAddresses(Dns.GetHostName()).Where(x=>x.AddressFamily==AddressFamily.InterNetwork)) if(!address.Items.Contains(ip.ToString())) address.Items.Add(ip.ToString()); } catch { }
            address.Text=settings.Host??"127.0.0.1";
            port=Theme.Number(settings.Port,1,65535); slots=Theme.Number(settings.MaxSessions,1,16); timeout=Theme.Number(settings.Timeout,1,600);
            line1.Controls.AddRange(new Control[]{Theme.Label("Listen IPv4"),address,Theme.Label("Port"),port,Theme.Label("Số phiên tối đa"),slots,Theme.Label("Timeout (s)"),timeout});
            root.Controls.Add(line1,0,1);
            var line2=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Padding=new Padding(3,6,0,0) };
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120)); line2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,160));
            folder=new TextBox { Text=settings.Folder??new ServerOptions().Folder,Dock=DockStyle.Fill,Margin=new Padding(4,7,7,4) }; browse=Theme.Button("Chọn thư mục");
            line2.Controls.Add(Theme.Label("Thư mục nhận"),0,0); line2.Controls.Add(folder,1,0); line2.Controls.Add(browse,2,0); root.Controls.Add(line2,0,2);
            var line3=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false };
            maxMiB=Theme.Number(settings.MaxMiB,0,10240,100); policy=Theme.Label("File trùng tên: từ chối, giữ nguyên file cũ.");
            line3.Controls.AddRange(new Control[]{Theme.Label("Giới hạn / file (MiB)"),maxMiB,policy}); root.Controls.Add(line3,0,3);
            var actions=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false };
            start=Theme.Button("Bật Server",true); stop=Theme.Button("Dừng Server"); open=Theme.Button("Mở thư mục nhận"); openLog=Theme.Button("Mở thư mục log");
            actions.Controls.AddRange(new Control[]{start,stop,open,openLog}); root.Controls.Add(actions,0,4);
            status=Theme.Label("Server chưa chạy. 127.0.0.1 chỉ nhận kết nối trên máy này.",true); root.Controls.Add(status,0,5);
            grid=Theme.Grid();
            Theme.Column(grid,"Id","Phiên",75); Theme.Column(grid,"Peer","Client",145); Theme.Column(grid,"Name","Tên file",180,true);
            Theme.Column(grid,"State","Trạng thái",105); Theme.Column(grid,"Bytes","Đã nhận / tổng",165); Theme.Column(grid,"Detail","Thông tin",220,true);
            root.Controls.Add(grid,0,6); log=Theme.LogBox(); log.Margin=new Padding(0,10,0,0); root.Controls.Add(log,0,7);
            browse.Click+=delegate { using(var d=new FolderBrowserDialog { SelectedPath=folder.Text,Description="Chọn thư mục riêng để nhận file" }) if(d.ShowDialog(this)==DialogResult.OK) folder.Text=d.SelectedPath; };
            open.Click+=delegate { Theme.OpenFolder(folder.Text); };
            openLog.Click+=delegate { if(engine!=null && !string.IsNullOrEmpty(engine.LogPath)) Theme.OpenFolder(Path.GetDirectoryName(engine.LogPath)); };
            start.Click+=delegate { StartServer(); };
            stop.Click+=async delegate { await StopServer(); };
            heartbeat.Interval=500; heartbeat.Tick+=delegate { RefreshStatus(); }; heartbeat.Start();
            FormClosing+=HandleFormClosing;
            Theme.Append(log,"Chỉ bật LAN khi cần: chọn 0.0.0.0 và cấp quyền tường lửa mạng Private. Không mở port Internet.");
            RefreshStatus();
        }
        private void SaveSettings()
        {
            settings.Host=address.Text.Trim(); settings.Port=(int)port.Value; settings.Timeout=(int)timeout.Value;
            settings.MaxSessions=(int)slots.Value; settings.MaxMiB=(int)maxMiB.Value; settings.Folder=folder.Text.Trim();
            try { settings.Save("server"); } catch(Exception ex) { Theme.Append(log,"Không lưu được cấu hình: "+ex.Message); }
        }
        private void StartServer()
        {
            try
            {
                var config=new ServerOptions { Address=address.Text.Trim(),Port=(int)port.Value,Folder=folder.Text.Trim(),MaxSessions=(int)slots.Value,TimeoutSeconds=(int)timeout.Value,MaxFileBytes=(long)maxMiB.Value*1024*1024 };
                config.Validate(); SaveSettings();
                var next=new ServerEngine();
                next.Log+=line => Theme.Post(this,()=>Theme.Append(log,line));
                next.Changed+=e => Theme.Post(this,()=>UpdateRecord(e));
                next.Start(config); engine=next;
            }
            catch(Exception ex) { MessageBox.Show(this,ex.Message,"Không bật được Server",MessageBoxButtons.OK,MessageBoxIcon.Error); Theme.Append(log,"START ERROR: "+ex.Message); }
            RefreshStatus();
        }
        private async Task StopServer()
        {
            if(engine==null || stopping) return;
            stopping=true; RefreshStatus();
            try { await engine.StopAsync(); }
            catch(Exception ex) { Theme.Append(log,"STOP ERROR: "+ex.Message); }
            finally { stopping=false; RefreshStatus(); if(closeAfter) Close(); }
        }
        private void RefreshStatus()
        {
            bool live=engine!=null && engine.IsRunning;
            start.Enabled=!live && !stopping; stop.Enabled=live && !stopping;
            address.Enabled=port.Enabled=slots.Enabled=timeout.Enabled=maxMiB.Enabled=folder.Enabled=browse.Enabled=!live && !stopping;
            openLog.Enabled=engine!=null && !string.IsNullOrEmpty(engine.LogPath);
            status.Text=stopping ? "Đang dừng, hủy phiên truyền và dọn file tạm..." : live ? "Đang lắng nghe "+address.Text+":"+engine.BoundPort+"  |  Phiên đang mở: "+engine.ActiveCount+" / "+slots.Value : "Server đã dừng / chưa bật.";
            status.ForeColor=live?Theme.Teal:Theme.Muted;
        }
        private void UpdateRecord(ServerEvent e)
        {
            DataGridViewRow row;
            if(!records.TryGetValue(e.Id,out row))
            {
                // Bound GUI history, but never evict a currently active row.
                if(grid.Rows.Count>=300)
                {
                    foreach(DataGridViewRow old in grid.Rows)
                    {
                        string state=Convert.ToString(old.Cells["State"].Value);
                        if(state=="Completed" || state=="Error") { records.Remove(Convert.ToString(old.Tag)); grid.Rows.Remove(old); break; }
                    }
                }
                int n=grid.Rows.Add(e.Id.Substring(0,8),e.Remote,e.Name,e.State,"",e.Detail);
                row=grid.Rows[n]; row.Tag=e.Id; records[e.Id]=row;
            }
            row.Cells["Name"].Value=e.Name; row.Cells["State"].Value=e.State;
            row.Cells["Bytes"].Value=FileRules.Bytes(e.Received)+" / "+FileRules.Bytes(e.Total);
            row.Cells["Detail"].Value=e.Detail; row.Cells["Detail"].ToolTipText=e.Detail;
            row.Cells["State"].Style.ForeColor=e.State=="Error"?Color.Firebrick:e.State=="Completed"?Theme.Teal:Theme.Ink;
            RefreshStatus();
        }
        private async void HandleFormClosing(object sender,FormClosingEventArgs e)
        {
            if(stopping) { e.Cancel=true; closeAfter=true; return; }
            if(engine!=null && engine.IsRunning)
            {
                e.Cancel=true;
                if(MessageBox.Show(this,"Dừng Server và mọi phiên tải đang chạy?", "Đóng Server",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return;
                closeAfter=true; await StopServer();
            }
            else { SaveSettings(); heartbeat.Stop(); heartbeat.Dispose(); }
        }
    }
}