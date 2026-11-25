using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Udm10.ServerCore;
using Udm10.Shared;

namespace Udm10.Headless
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding=Encoding.UTF8;
            try
            {
                if(args.Length==0) { Usage(); return 1; }
                using(var cancellation=new CancellationTokenSource())
                {
                    Console.CancelKeyPress+=(sender,e) => { e.Cancel=true; cancellation.Cancel(); };
                    if(args[0]=="server" && args.Length>=2 && args.Length<=4)
                    {
                        var options=new ServerOptions { Folder=Path.GetFullPath(args[1]),Port=args.Length>2 ? int.Parse(args[2]) : 5000,
                            Address=args.Length>3 ? args[3] : "127.0.0.1" };
                        var server=new ServerEngine();
                        server.Log+=Console.WriteLine;
                        server.Start(options);
                        Console.WriteLine("Nhấn Ctrl+C để dừng. Log: "+server.LogPath);
                        try { await Task.Delay(Timeout.Infinite,cancellation.Token); }
                        catch(OperationCanceledException) { }
                        finally { await server.StopAsync(); }
                        return 0;
                    }
                    if(args[0]=="upload" && args.Length==4)
                    {
                        var result=await new UploadClientService().UploadAsync(new UploadJob(args[3]),
                            new ClientOptions { Host=args[1],Port=int.Parse(args[2]) },
                            update => Console.WriteLine(update.State+" "+update.Percent+"%"),cancellation.Token);
                        Console.WriteLine("COMPLETE "+result.Name+" sha256="+result.Sha256);
                        return 0;
                    }
                    if(args[0]=="ping" && args.Length==3)
                    {
                        await new UploadClientService().ProbeAsync(new ClientOptions { Host=args[1],Port=int.Parse(args[2]) },cancellation.Token);
                        Console.WriteLine("PONG"); return 0;
                    }
                    Usage(); return 1;
                }
            }
            catch(OperationCanceledException) { Console.Error.WriteLine("Đã hủy."); return 2; }
            catch(Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }
        private static void Usage()
        {
            Console.WriteLine("server <thư mục lưu> [cổng=5000] [IPv4=127.0.0.1]");
            Console.WriteLine("upload <IPv4> <cổng> <đường dẫn file>");
            Console.WriteLine("ping <IPv4> <cổng>");
        }
    }
}
