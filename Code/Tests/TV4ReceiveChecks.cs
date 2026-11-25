using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Udm10.ServerCore;
using Udm10.Shared;

namespace Udm10.Tv4Checks
{
    // Real loopback sockets; no third-party test packages or simulated PASS results.
    internal static class Tv4ReceiveChecks
    {
        private static string root;
        private static readonly CancellationToken None=CancellationToken.None;
        private static readonly byte[] Payload=Enumerable.Range(0,180000).Select(i => (byte)(i*31)).ToArray();

        internal static async Task<int> Run(string[] args)
        {
            root=Path.Combine(Path.GetFullPath(args.Length==0 ? "BuildLogs/TV4" : args[0]),
                DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
            Directory.CreateDirectory(root);
            var tests=new List<(string Id,Func<Task> Run)> {
                ("RECEIVE_EXACT_AND_EMPTY",ReceiveExactAndEmpty),
                ("TC_12_DUPLICATE",Duplicate), ("TC_15_SIZE_LIMIT",SizeLimit),
                ("TC_16_INVALID_NAMES",InvalidNames), ("TC_17_BAD_JSON",BadJson),
                ("TC_18_BAD_LENGTH",BadLength), ("TC_19_NEGATIVE_SIZE",NegativeSize),
                ("TC_20_DISCONNECT",Disconnect), ("TC_21_TIMEOUT",Timeout),
                ("TC_22_HASH_MISMATCH",HashMismatch), ("TC_23_THREE_CLIENTS",ThreeClients),
                ("TC_24_BUSY",Busy), ("TC_25_NAME_RESERVATION",NameReservation),
                ("TC_27_STOP_RESTART",StopRestart), ("TC_28_STALE_PARTS",StaleParts),
                ("TC_29_FOLDER_LOCK",FolderLock), ("CLIENT_ISOLATION",ClientIsolation),
                ("COMMIT_IO_FAILURE",CommitIoFailure), ("BAD_END",BadEnd),
                ("OBSERVER_ISOLATION",ObserverIsolation), ("TV3_SHARED_CLIENT",SharedClient)
            };
            int failures=0;
            using(var report=new StreamWriter(Path.Combine(root,"results.txt"),false,new UTF8Encoding(false)))
            {
                void Write(string text) { Console.WriteLine(text); report.WriteLine(text); report.Flush(); }
                Write("TV4 TCP integration tests | "+DateTimeOffset.Now.ToString("O"));
                Write("OS: "+System.Runtime.InteropServices.RuntimeInformation.OSDescription);
                Write("Runtime: "+System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
                Write("Evidence: "+root);
                foreach(var test in tests)
                {
                    try { await test.Run().WaitAsync(TimeSpan.FromSeconds(20)); Write("PASS "+test.Id); }
                    catch(Exception ex) { failures++; Write("FAIL "+test.Id+" | "+ex); }
                }
                Write("RESULT: "+(tests.Count-failures)+"/"+tests.Count+" PASS; "+failures+" FAIL");
            }
            return failures==0 ? 0 : 1;
        }

        private sealed class Fixture : IAsyncDisposable
        {
            public readonly ServerEngine Server=new ServerEngine();
            public readonly ServerOptions Options;
            public string Folder { get { return Options.Folder; } }
            public Fixture(Action<ServerOptions> configure=null,Action<string> prepare=null,Action<ServerEngine> observe=null)
            {
                Options=new ServerOptions { Folder=Path.Combine(root,Guid.NewGuid().ToString("N")),Port=0,TimeoutSeconds=3,MaxFileBytes=1024*1024 };
                configure?.Invoke(Options);
                Directory.CreateDirectory(Folder); prepare?.Invoke(Folder); observe?.Invoke(Server);
                Server.Start(Options);
            }
            public async ValueTask DisposeAsync() { await Server.StopAsync(); }
            public async Task Idle()
            {
                await Until(() => Server.ActiveCount==0);
                Check(!Directory.EnumerateFiles(Path.Combine(Folder,".udm10-incoming"),"*.part").Any(),"Temporary file leaked");
            }
            public async Task Healthy()
            {
                await Idle();
                await new UploadClientService().ProbeAsync(new ClientOptions { Port=Server.BoundPort,TimeoutSeconds=3 },None);
                await Idle();
            }
        }
        private sealed class Peer : IDisposable
        {
            public readonly TcpClient Client=new TcpClient(AddressFamily.InterNetwork);
            public string Id;
            public string Name;
            public static async Task<Peer> Open(Fixture fixture)
            {
                var peer=new Peer();
                try
                {
                    await peer.Client.ConnectAsync(IPAddress.Loopback,fixture.Server.BoundPort);
                    Check((await peer.Read()).Type=="WELCOME","Missing WELCOME"); return peer;
                }
                catch { peer.Dispose(); throw; }
            }
            public Task<Message> Read() { return Wire.ReadMessage(Client,4,None); }
            public Task Write(Message message) { return Wire.WriteMessage(Client,message,4,None); }
            public async Task Begin(string name,long size)
            {
                Name=name; await Write(new Message { Type="UPLOAD",Name=name,Size=size });
                Message ready=await Read(); Check(ready.Type=="READY","Expected READY: "+ready.Code); Id=ready.Id;
                Check(!string.IsNullOrEmpty(Id),"Missing transfer ID");
            }
            public async Task<Message> Finish(byte[] bytes,string hash=null)
            {
                if(bytes.Length>0) await Wire.WriteBytes(Client,bytes,0,bytes.Length,4,None);
                await Write(new Message { Type="END",Id=Id,Sha256=hash??Digest(bytes) });
                return await Read();
            }
            public void Dispose() { Client.Dispose(); }
        }
        private static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
        private static string Digest(byte[] bytes) { return FileRules.Hex(SHA256.HashData(bytes)); }
        private static async Task Until(Func<bool> condition)
        {
            DateTime deadline=DateTime.UtcNow.AddSeconds(5);
            while(!condition()) { if(DateTime.UtcNow>deadline) throw new Exception("Server did not settle"); await Task.Delay(10); }
        }
        private static void Error(Message message,string code)
        { Check(message.Type=="ERROR" && message.Code==code,"Expected "+code+", got "+message.Type+" / "+message.Code); }
        private static async Task Upload(Fixture f,string name,byte[] bytes)
        {
            using(var peer=await Peer.Open(f))
            {
                await peer.Begin(name,bytes.Length);
                Message result=await peer.Finish(bytes);
                Check(result.Type=="COMPLETE" && result.Size==bytes.Length && result.Sha256==Digest(bytes),"Invalid COMPLETE");
            }
            Check(File.ReadAllBytes(Path.Combine(f.Folder,name)).SequenceEqual(bytes),"Saved bytes differ");
        }
        private static async Task Reject(Fixture f,Message request,string code)
        {
            using(var peer=await Peer.Open(f)) { await peer.Write(request); Error(await peer.Read(),code); }
            await f.Healthy();
        }
        private static async Task ReceiveExactAndEmpty()
        {
            await using var f=new Fixture();
            using(var peer=await Peer.Open(f))
            {
                byte[] body=Wire.Serialize(new Message { Type="UPLOAD",Name="tiếng Việt.bin",Size=Payload.Length });
                byte[] prefix=BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length));
                // Split every prefix byte and JSON fragment across separate socket writes.
                foreach(byte b in prefix) { await Wire.WriteBytes(peer.Client,new[] { b },0,1,4,None); await Task.Delay(2); }
                for(int i=0;i<body.Length;i+=3) await Wire.WriteBytes(peer.Client,body,i,Math.Min(3,body.Length-i),4,None);
                Message ready=await peer.Read(); Check(ready.Type=="READY","Fragmented header rejected"); peer.Id=ready.Id;
                for(int i=0;i<Payload.Length;i+=997) await Wire.WriteBytes(peer.Client,Payload,i,Math.Min(997,Payload.Length-i),4,None);
                await peer.Write(new Message { Type="END",Id=peer.Id,Sha256=Digest(Payload) });
                Check((await peer.Read()).Type=="COMPLETE","Split upload failed");
            }
            Check(File.ReadAllBytes(Path.Combine(f.Folder,"tiếng Việt.bin")).SequenceEqual(Payload),"Fragmented payload corrupted");
            await f.Idle(); await Upload(f,"empty.bin",Array.Empty<byte>()); await f.Healthy();
        }
        private static async Task Duplicate()
        {
            await using var f=new Fixture(); await Upload(f,"same.bin",Payload); await f.Idle();
            await Reject(f,new Message { Type="UPLOAD",Name="SAME.bin",Size=1 },"DUPLICATE");
            Check(File.ReadAllBytes(Path.Combine(f.Folder,"same.bin")).SequenceEqual(Payload),"Existing file overwritten");
        }
        private static async Task SizeLimit()
        {
            await using var f=new Fixture(o => o.MaxFileBytes=8);
            await Reject(f,new Message { Type="UPLOAD",Name="large.bin",Size=9 },"TOO_LARGE");
            Check(!File.Exists(Path.Combine(f.Folder,"large.bin")),"Oversized file committed");
        }
        private static async Task InvalidNames()
        {
            await using var f=new Fixture();
            foreach(string name in new[] { "../outside.txt","..\\outside.txt","C:\\outside.txt","/outside.txt","CON","nul.txt","COM1.txt","LPT9","bad:ads","bad.","bad ",".udm10-server.lock","",new string('x',181) })
                await Reject(f,new Message { Type="UPLOAD",Name=name,Size=0 },"INVALID_NAME");
            Check(Directory.GetFiles(f.Folder).All(p => Path.GetFileName(p)==".udm10-server.lock"),"Invalid file created");
        }
        private static async Task BadJson()
        {
            await using var f=new Fixture(); using(var peer=await Peer.Open(f))
            {
                byte[] bytes={ 0,0,0,1,(byte)'{' }; await Wire.WriteBytes(peer.Client,bytes,0,bytes.Length,4,None);
                Error(await peer.Read(),"BAD_JSON");
            }
            await f.Healthy();
        }
        private static async Task BadLength()
        {
            await using var f=new Fixture();
            foreach(int length in new[] { 0,Wire.MaxHeaderBytes+1,-1 })
            {
                using(var peer=await Peer.Open(f))
                {
                    byte[] prefix=BitConverter.GetBytes(IPAddress.HostToNetworkOrder(length));
                    await Wire.WriteBytes(peer.Client,prefix,0,4,4,None); Error(await peer.Read(),"BAD_LENGTH");
                }
                await f.Healthy();
            }
        }
        private static async Task NegativeSize()
        {
            await using var f=new Fixture();
            await Reject(f,new Message { Type="UPLOAD",Name="negative.bin",Size=-1 },"BAD_SIZE");
            await Reject(f,new Message { Type="UPLOAD",Name="missing.bin" },"BAD_SIZE");
        }
        private static async Task Disconnect()
        {
            await using var f=new Fixture();
            using(var peer=await Peer.Open(f)) { await peer.Begin("partial.bin",100); await Wire.WriteBytes(peer.Client,new byte[5],0,5,4,None); }
            await f.Healthy(); Check(!File.Exists(Path.Combine(f.Folder,"partial.bin")),"Partial file committed");
            await Upload(f,"partial.bin",new byte[2]);
        }
        private static async Task Timeout()
        {
            await using var f=new Fixture(o => o.TimeoutSeconds=1);
            using(var peer=await Peer.Open(f))
            {
                await peer.Begin("idle.bin",10); await f.Idle();
                Check(!File.Exists(Path.Combine(f.Folder,"idle.bin")),"Timed-out file committed");
                using(var log=new StreamReader(new FileStream(f.Server.LogPath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)))
                    Check(log.ReadToEnd().Contains("code=TIMEOUT"),"No timeout log");
            }
            await f.Healthy();
        }
        private static async Task HashMismatch()
        {
            await using var f=new Fixture(); using(var peer=await Peer.Open(f))
            { await peer.Begin("hash.bin",Payload.Length); Error(await peer.Finish(Payload,new string('0',64)),"HASH_MISMATCH"); }
            await f.Healthy(); Check(!File.Exists(Path.Combine(f.Folder,"hash.bin")),"Bad hash committed");
        }
        private static async Task ThreeClients()
        {
            await using var f=new Fixture(o => o.MaxSessions=3);
            byte[] second=Payload.Reverse().ToArray();
            byte[] third=Payload.Select(b => (byte)(b^0x5a)).ToArray();
            using var a=await Peer.Open(f); using var b=await Peer.Open(f); using var c=await Peer.Open(f);
            await Task.WhenAll(a.Begin("a.bin",Payload.Length),b.Begin("b.bin",second.Length),c.Begin("c.bin",third.Length));
            Check(f.Server.ActiveCount==3,"Expected three concurrent sessions");
            Message[] results=await Task.WhenAll(a.Finish(Payload),b.Finish(second),c.Finish(third));
            Check(results.All(r => r.Type=="COMPLETE"),"Concurrent upload failed");
            foreach(var file in new[] { (Name:"a.bin",Bytes:Payload),(Name:"b.bin",Bytes:second),(Name:"c.bin",Bytes:third) })
                Check(File.ReadAllBytes(Path.Combine(f.Folder,file.Name)).SequenceEqual(file.Bytes),"Mixed client data: "+file.Name);
            await f.Healthy();
        }
        private static async Task Busy()
        {
            await using var f=new Fixture(o => o.MaxSessions=1);
            using(var a=await Peer.Open(f))
            {
                await a.Begin("held.bin",Payload.Length);
                using(var extra=new TcpClient())
                {
                    await extra.ConnectAsync(IPAddress.Loopback,f.Server.BoundPort);
                    Error(await Wire.ReadMessage(extra,3,None),"BUSY");
                }
                Check((await a.Finish(Payload)).Type=="COMPLETE","BUSY damaged existing session");
            }
            await f.Healthy();
        }
        private static async Task NameReservation()
        {
            await using var f=new Fixture();
            using(var a=await Peer.Open(f))
            {
                await a.Begin("race.bin",100);
                using(var b=await Peer.Open(f))
                { await b.Write(new Message { Type="UPLOAD",Name="RACE.bin",Size=100 }); Error(await b.Read(),"DUPLICATE"); }
            }
            await f.Idle(); await Upload(f,"race.bin",Payload); await f.Healthy();
        }
        private static async Task StopRestart()
        {
            await using var f=new Fixture();
            using var peer=await Peer.Open(f); await peer.Begin("stopped.bin",100);
            await Wire.WriteBytes(peer.Client,new byte[5],0,5,4,None);
            Task first=f.Server.StopAsync(); Task second=f.Server.StopAsync();
            Check(ReferenceEquals(first,second),"Stop callers must await the same shutdown");
            await Task.WhenAll(first,second); await f.Idle();
            Check(!f.Server.IsRunning && !File.Exists(Path.Combine(f.Folder,"stopped.bin")),"Stop left an upload");
            var replacement=new ServerEngine();
            try { replacement.Start(f.Options); Check(replacement.IsRunning,"Restart failed"); }
            finally { await replacement.StopAsync(); }
        }
        private static async Task StaleParts()
        {
            string owned="udm10-"+Guid.NewGuid().ToString("N")+".part";
            await using var f=new Fixture(prepare:folder => {
                string incoming=Path.Combine(folder,".udm10-incoming"); Directory.CreateDirectory(incoming);
                File.WriteAllText(Path.Combine(incoming,owned),"partial");
                File.WriteAllText(Path.Combine(incoming,"other.part"),"preserve");
                File.WriteAllText(Path.Combine(incoming,"udm10-invalid.part"),"preserve");
            });
            string temp=Path.Combine(f.Folder,".udm10-incoming");
            Check(!File.Exists(Path.Combine(temp,owned)),"Owned stale file remains");
            Check(File.ReadAllText(Path.Combine(temp,"other.part"))=="preserve" && File.Exists(Path.Combine(temp,"udm10-invalid.part")),"Unowned file removed");
            // This case deliberately keeps unowned .part files; do not call Idle's empty-part assertion.
            await new UploadClientService().ProbeAsync(new ClientOptions { Port=f.Server.BoundPort },None);
        }
        private static async Task FolderLock()
        {
            await using var f=new Fixture(); var other=new ServerEngine(); bool rejected=false;
            try { other.Start(f.Options); } catch(IOException) { rejected=true; }
            finally { await other.StopAsync(); }
            Check(rejected,"Second writer acquired storage lock"); await f.Healthy();
        }
        private static async Task ClientIsolation()
        {
            await using var f=new Fixture(); using var b=await Peer.Open(f); await b.Begin("survivor.bin",Payload.Length);
            using(var a=await Peer.Open(f)) { await a.Begin("broken.bin",100); await Wire.WriteBytes(a.Client,new byte[1],0,1,4,None); }
            Check((await b.Finish(Payload)).Type=="COMPLETE","Client A failure affected B");
            await f.Healthy(); Check(File.ReadAllBytes(Path.Combine(f.Folder,"survivor.bin")).SequenceEqual(Payload),"Survivor bytes differ");
        }
        private static async Task CommitIoFailure()
        {
            await using var f=new Fixture(); using(var peer=await Peer.Open(f))
            {
                await peer.Begin("occupied.bin",Payload.Length);
                File.WriteAllText(Path.Combine(f.Folder,"occupied.bin"),"external content");
                Error(await peer.Finish(Payload),"IO_ERROR");
            }
            await f.Healthy(); Check(File.ReadAllText(Path.Combine(f.Folder,"occupied.bin"))=="external content","Commit overwrote external file");
        }
        private static async Task BadEnd()
        {
            await using var f=new Fixture(); using(var peer=await Peer.Open(f))
            {
                await peer.Begin("end.bin",0);
                await peer.Write(new Message { Type="END",Id="wrong",Sha256=Digest(Array.Empty<byte>()) }); Error(await peer.Read(),"BAD_END");
            }
            await f.Healthy(); Check(!File.Exists(Path.Combine(f.Folder,"end.bin")),"Invalid END committed");
        }
        private static async Task ObserverIsolation()
        {
            int events=0,logs=0,emptyProgress=0;
            await using var f=new Fixture(observe:server => {
                server.Changed+=e => throw new InvalidOperationException("UI failure");
                server.Changed+=e => { Interlocked.Increment(ref events); if(e.State=="Uploading" && e.Total==0) Interlocked.Increment(ref emptyProgress); };
                server.Log+=line => throw new InvalidOperationException("Log UI failure");
                server.Log+=line => Interlocked.Increment(ref logs);
            });
            await Upload(f,"observer.bin",Array.Empty<byte>()); await f.Healthy();
            Check(events>0 && logs>0 && emptyProgress==1,"Throwing observer suppressed another subscriber or empty-file progress");
        }
        private static async Task SharedClient()
        {
            await using var f=new Fixture(); string source=Path.Combine(root,"tv3-"+Guid.NewGuid().ToString("N")+".bin");
            File.WriteAllBytes(source,Payload);
            UploadResult result=await new UploadClientService().UploadAsync(new UploadJob(source),new ClientOptions { Port=f.Server.BoundPort },null,None);
            Check(result.Sha256==Digest(Payload) && File.ReadAllBytes(Path.Combine(f.Folder,Path.GetFileName(source))).SequenceEqual(Payload),"Shared client incompatible");
            await f.Healthy();
        }
    }
}
