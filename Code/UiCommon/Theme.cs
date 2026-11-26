using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Udm10.Shared;

namespace Udm10.UI
{
    internal static class Theme
    {
        internal static readonly Color Ink=Color.FromArgb(20,38,58);
        internal static readonly Color Teal=Color.FromArgb(0,117,112);
        internal static readonly Color Paper=Color.FromArgb(244,247,250);
        internal static readonly Color Muted=Color.FromArgb(88,108,125);
        internal static void Form(Form f,string title)
        {
            f.Text=title; f.Font=new Font("Segoe UI",10F); f.BackColor=Paper;
            f.StartPosition=FormStartPosition.CenterScreen;
            f.AutoScaleMode=AutoScaleMode.Dpi; f.MinimumSize=new Size(1060,740); f.Size=new Size(1180,820);
        }
        internal static Label Label(string text,bool bold=false)
        { return new Label { Text=text,AutoSize=true,ForeColor=Ink,Font=new Font("Segoe UI",10F,bold?FontStyle.Bold:FontStyle.Regular),Margin=new Padding(4,8,8,4) }; }
        internal static Button Button(string text,bool primary=false)
        {
            var b=new Button { Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(12,7,12,7),Margin=new Padding(5),FlatStyle=FlatStyle.Flat,
                BackColor=primary?Teal:Color.White,ForeColor=primary?Color.White:Ink,Cursor=Cursors.Hand };
            b.FlatAppearance.BorderColor=Color.FromArgb(205,215,224); return b;
        }
        internal static Panel Header(string title,string subtitle)
        {
            var p=new Panel { Dock=DockStyle.Fill,BackColor=Ink,Padding=new Padding(20,10,20,8) };
            p.Controls.Add(new Label { Text=subtitle,ForeColor=Color.FromArgb(195,213,229),Dock=DockStyle.Bottom,Height=24,AutoEllipsis=true });
            p.Controls.Add(new Label { Text=title,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",22F),Dock=DockStyle.Top,Height=42 });
            return p;
        }
        internal static NumericUpDown Number(int value,int min,int max,int width=85)
        { return new NumericUpDown { Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),Width=width,Margin=new Padding(4,6,12,4) }; }
        internal static DataGridView Grid()
        {
            var g=new DataGridView { Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,ReadOnly=true,
                RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=true,
                BackgroundColor=Color.White,BorderStyle=BorderStyle.None,AutoGenerateColumns=false,
                EnableHeadersVisualStyles=false,ColumnHeadersHeight=38,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing };
            g.DefaultCellStyle.Font=new Font("Segoe UI",9.5F); g.RowTemplate.Height=35;
            g.DefaultCellStyle.SelectionBackColor=Color.FromArgb(218,238,236); g.DefaultCellStyle.SelectionForeColor=Ink;
            g.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(248,250,252);
            g.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(227,234,241); g.ColumnHeadersDefaultCellStyle.ForeColor=Ink;
            g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI Semibold",10F);
            g.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal; g.GridColor=Color.FromArgb(230,236,241);
            return g;
        }
        internal static void Column(DataGridView g,string key,string title,int width,bool fill=false)
        { g.Columns.Add(new DataGridViewTextBoxColumn { Name=key,HeaderText=title,Width=width,MinimumWidth=width,AutoSizeMode=fill?DataGridViewAutoSizeColumnMode.Fill:DataGridViewAutoSizeColumnMode.None,SortMode=DataGridViewColumnSortMode.NotSortable }); }
        internal static TextBox LogBox()
        { return new TextBox { Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,BackColor=Color.White,Font=new Font("Consolas",9F),BorderStyle=BorderStyle.FixedSingle }; }
        internal static void Append(TextBox log,string line)
        {
            if(log.TextLength>60000) log.Text=log.Text.Substring(log.TextLength/2);
            log.AppendText(line+Environment.NewLine);
        }
        internal static void Post(Control control,Action action)
        {
            if(control.IsDisposed || !control.IsHandleCreated) return;
            try { if(control.InvokeRequired) control.BeginInvoke(action); else action(); }
            catch(InvalidOperationException) { }
        }
        internal static void OpenFolder(string folder)
        {
            try { if(!Directory.Exists(folder)) Directory.CreateDirectory(folder); Process.Start(new ProcessStartInfo(folder) { UseShellExecute=true }); }
            catch(Exception ex) { MessageBox.Show(ex.Message,"Cannot open folder",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
        }
        internal static void InstallErrorHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException+=delegate(object sender,System.Threading.ThreadExceptionEventArgs args)
            {
                TryLogFatal(args.Exception);
                MessageBox.Show("Unexpected UI error. Stop the current operation and inspect the error log.\n"+args.Exception.Message,"UDM10",MessageBoxButtons.OK,MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException+=delegate(object sender,UnhandledExceptionEventArgs args) { TryLogFatal(args.ExceptionObject as Exception); };
        }
        private static void TryLogFatal(Exception ex)
        {
            try { Directory.CreateDirectory(AppSettings.SettingsFolder); File.AppendAllText(Path.Combine(AppSettings.SettingsFolder,"errors.log"),DateTime.Now+" "+ex+Environment.NewLine); } catch { }
        }
    }
}
