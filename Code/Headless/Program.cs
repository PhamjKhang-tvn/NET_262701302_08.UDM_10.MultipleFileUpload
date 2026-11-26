using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Udm10.Shared;
using Udm10.ServerCore;

namespace Udm10.Headless
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try { return Run(args).GetAwaiter().GetResult(); }
            catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
        private static async Task<int> Run(string[] args)
        {
            if(args.Length==0)
            {
                Console.WriteLine("UDM10 test helper. Normal product uses the two GUI executables.");
                Console.WriteLine("serve <folder> [port=5000] [sessions=3] [timeout=30] [ready-file]");
                Console.WriteLine("send <IPv4> <port> <file1> [file2...]");
                return 2;
            }
            if(args[0]=="serve")
            {
                if(args.Length<2) throw new ArgumentException("Storage folder is required.");
                var options=new ServerOptions { Folder=args[1],Address="127.0.0.1",Port=args.Length>2?int.Parse(args[2]):5000,MaxSessions=args.Length>3?int.Parse(args[3]):3,TimeoutSeconds=args.Length>4?int.Parse(args[4]):30 };
                var server=new ServerEngine(); server.Log+=Console.WriteLine;
                server.Start(options);
                if(args.Length>5) { File.WriteAllText(args[5]+".tmp",server.BoundPort.ToString()); File.Move(args[5]+".tmp",args[5]); }
                var stop=new TaskCompletionSource<bool>();
                Console.CancelKeyPress+=delegate(object sender,ConsoleCancelEventArgs e) { e.Cancel=true; stop.TrySetResult(true); };
                Task input=Task.Run(() => { while(true) { string line=Console.ReadLine(); if(line==null || line.Trim().Equals("STOP",StringComparison.OrdinalIgnoreCase)) { stop.TrySetResult(true); break; } } });
                Console.WriteLine("READY "+server.BoundPort+"; enter STOP to end.");
                await stop.Task;
                await server.StopAsync(); return 0;
            }
            if(args[0]=="send")
            {
                if(args.Length<4) throw new ArgumentException("send requires IP, port and at least one file.");
                var options=new ClientOptions { Host=args[1],Port=int.Parse(args[2]) };
                var jobs=new System.Collections.Generic.List<UploadJob>();
                for(int i=3;i<args.Length;i++) jobs.Add(new UploadJob(args[i]));
                var result=await new UploadQueue().RunAsync(jobs,options,u => Console.WriteLine(u.JobId+" "+u.State+" "+u.Percent+"% "+u.Detail),CancellationToken.None);
                return result.Errors==0 && result.Waiting==0 ? 0 : 1;
            }
            throw new ArgumentException("Unknown command.");
        }
    }
}
