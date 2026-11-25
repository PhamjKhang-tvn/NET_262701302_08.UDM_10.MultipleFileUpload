using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Udm10.Shared;
using Udm10.ServerCore;

namespace Udm10.Tests
{
    [DataContract]
    public sealed class TestRecord
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public string Stage;
        [DataMember] public string Actual;
        [DataMember] public double Seconds;
    }
    [DataContract]
    public sealed class Measurement
    {
        [DataMember] public string Id;
        [DataMember] public int Clients;
        [DataMember] public int Files;
        [DataMember] public long Bytes;
        [DataMember] public double Seconds;
        [DataMember] public double MiBPerSecond;
        [DataMember] public int Errors;
        [DataMember] public long WorkingSetBytes;
    }
    [DataContract]
    public sealed class TestRun
    {
        [DataMember] public string StartedUtc;
        [DataMember] public string Environment;
        [DataMember] public string Scope;
        [DataMember] public List<TestRecord> Records=new List<TestRecord>();
        [DataMember] public List<Measurement> Measurements=new List<Measurement>();
    }
    internal sealed class Fixture
    {
        internal string Root,Source,Destination,Id;
        internal ServerEngine Server;
        internal ClientOptions Client;
        internal Fixture(string id,int sessions=3,int timeout=2,long maxBytes=16777216)
        {
            Id=id; Root=Path.Combine(Path.GetTempPath(),"UDM10-TEST-"+Guid.NewGuid().ToString("N"));
            Source=Path.Combine(Root,"source"); Destination=Path.Combine(Root,"server"); Directory.CreateDirectory(Source);
            Server=new ServerEngine(); Server.Start(new ServerOptions { Folder=Destination,Address="127.0.0.1",Port=0,TimeoutSeconds=timeout,MaxSessions=sessions,MaxFileBytes=maxBytes });
            Client=new ClientOptions { Host="127.0.0.1",Port=Server.BoundPort,TimeoutSeconds=5 };
        }
        internal string File(string name,int size)
        {
            string path=Path.Combine(Source,name);
            byte[] buffer=new byte[Math.Min(Math.Max(size,1),65536)]; new Random(1749).NextBytes(buffer);
            using(var output=System.IO.File.Create(path))
            { int remain=size; while(remain>0) { int n=Math.Min(buffer.Length,remain); output.Write(buffer,0,n); remain-=n; } }
            return path;
        }
        internal async Task Close()
        {
            await Server.StopAsync();
            try { if(!string.IsNullOrEmpty(Server.LogPath)) System.IO.File.Copy(Server.LogPath,Path.Combine(Program.Output,Id+"-server.log"),true); } catch { }
            try { Directory.Delete(Root,true); } catch { }
        }
    }
    internal sealed class RawSession : IDisposable
    {
        internal TcpClient Client;
        internal Message Ready;
        public void Dispose() { Client.Close(); }
    }
    internal static class Program
    {
        internal static string Output;
        private static readonly TestRun run=new TestRun();
        private static string currentId;
        private static string detail;
        [DllImport("kernel32.dll",SetLastError=true)] private static extern bool GetPhysicallyInstalledSystemMemory(out ulong totalKb);
        private static int Main(string[] args)
        {
            if(args.Length>0 && args[0]=="--tv4")
                return Udm10.Tv4Checks.Tv4ReceiveChecks.Run(args.Skip(1).ToArray()).GetAwaiter().GetResult();
            Output=Path.GetFullPath(args.Length>0?args[0]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"TestResults",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
            Directory.CreateDirectory(Output);
            run.StartedUtc=DateTime.UtcNow.ToString("o");
            ulong memory=0; try { GetPhysicallyInstalledSystemMemory(out memory); } catch { }
            run.Environment=Environment.OSVersion+"; CLR="+Environment.Version+"; x64="+Environment.Is64BitProcess+"; CPU="+Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")+"; logicalCPUs="+Environment.ProcessorCount+"; installedRAM_KiB="+memory;
            run.Scope="TCP integration on 127.0.0.1; TC_36 uses separate OS processes. No GUI/LAN acceptance is claimed. Performance is loopback, not real LAN bandwidth.";
            try { RunAll().GetAwaiter().GetResult(); }
            catch(Exception ex) { run.Records.Add(new TestRecord { Id="HARNESS",Name="Harness error",Stage="FAILED",Actual=ex.ToString() }); }
            Save();
            int failed=run.Records.Count(r=>r.Stage=="FAILED");
            Console.WriteLine("\nPassed="+run.Records.Count(r=>r.Stage=="PASSED")+" Failed="+failed+" Evidence="+Output);
            return failed==0?0:1;
        }
        private static void Assert(bool truth,string text) { if(!truth) throw new InvalidOperationException(text); }
        private static string Hash(string path)
        { using(var h=SHA256.Create()) using(var f=System.IO.File.OpenRead(path)) return FileRules.Hex(h.ComputeHash(f)); }
        private static async Task WaitFor(Func<bool> condition,int timeout=5000)
        { var sw=Stopwatch.StartNew(); while(!condition()) { if(sw.ElapsedMilliseconds>timeout) throw new TimeoutException("Condition not reached."); await Task.Delay(25); } }
        private static async Task Test(string id,string name,Func<Task> body)
        {
            currentId=id; detail="All assertions passed."; var watch=Stopwatch.StartNew();
            var record=new TestRecord { Id=id,Name=name,Stage="PASSED" };
            try { await body(); record.Actual=detail; }
            catch(Exception ex) { record.Stage="FAILED"; record.Actual=ex.ToString(); }
            record.Seconds=watch.Elapsed.TotalSeconds; run.Records.Add(record);
            System.IO.File.WriteAllText(Path.Combine(Output,id+".txt"),record.Stage+" | "+name+Environment.NewLine+run.Environment+Environment.NewLine+record.Actual,Encoding.UTF8);
            Console.WriteLine(id+" "+record.Stage+" "+name+" ("+record.Seconds.ToString("0.000")+" s)");
        }
        private static Task Case(string id,string name,Func<Fixture,Task> body,int sessions=3,int timeout=2,long max=16777216)
        {
            return Test(id,name,async delegate
            {
                Fixture f=new Fixture(id,sessions,timeout,max); Exception failure=null;
                try { await body(f); } catch(Exception ex) { failure=ex; }
                await f.Close();
                if(failure!=null) ExceptionDispatchInfo.Capture(failure).Throw();
            });
        }
        private static async Task<TcpClient> Connect(Fixture f)
        {
            var c=new TcpClient(); await c.ConnectAsync(f.Client.Host,f.Client.Port);
            Message hello=await Wire.ReadMessage(c,5,CancellationToken.None); Assert(hello.Type=="WELCOME","Expected WELCOME, got "+hello.Type); return c;
        }
        private static async Task<RawSession> Raw(Fixture f,string name,long size)
        {
            var c=await Connect(f);
            await Wire.WriteMessage(c,new Message { Type="UPLOAD",Name=name,Size=size },5,CancellationToken.None);
            Message ready=await Wire.ReadMessage(c,5,CancellationToken.None);
            Assert(ready.Type=="READY","Expected READY, got "+ready.Type+" "+ready.Code);
            return new RawSession { Client=c,Ready=ready };
        }
        private static async Task Reject(Fixture f,Message message,string code)
        {
            using(var c=await Connect(f))
            {
                await Wire.WriteMessage(c,message,5,CancellationToken.None);
                Message answer=await Wire.ReadMessage(c,5,CancellationToken.None);
                Assert(answer.Type=="ERROR" && answer.Code==code,"Expected "+code+", got "+answer.Code);
            }
        }
        private static async Task<UploadResult> Upload(Fixture f,string path,Action<TransferUpdate> progress=null)
        { return await new UploadClientService().UploadAsync(new UploadJob(path),f.Client,progress,CancellationToken.None); }
        private static async Task RunAll()
        {
            await Case("TC_09","Binary transfer and SHA-256",async f=>
            {
                string src=f.File("binary.dat",1048576); var result=await Upload(f,src);
                Assert(Hash(src)==Hash(Path.Combine(f.Destination,"binary.dat")),"Hash mismatch.");
                Assert(result.Bytes==1048576,"Wrong size."); detail="1 MiB transferred; SHA-256="+result.Sha256;
            });
            await Case("TC_10","Empty file",async f=>
            { await Upload(f,f.File("empty.bin",0)); Assert(new FileInfo(Path.Combine(f.Destination,"empty.bin")).Length==0,"Not empty."); });
            await Case("TC_11","Unicode filename",async f=>
            { string name="B\u00e1o c\u00e1o m\u1ea1ng.txt"; string src=f.File(name,1234); await Upload(f,src); Assert(Hash(src)==Hash(Path.Combine(f.Destination,name)),"Unicode name/hash changed."); });
            await Case("TC_12","Duplicate never overwrites",async f=>
            {
                string src=f.File("keep.bin",32); await Upload(f,src); await WaitFor(()=>f.Server.ActiveCount==0); string before=Hash(Path.Combine(f.Destination,"keep.bin"));
                f.File("keep.bin",100);
                bool rejected=false; try { await Upload(f,src); } catch(UploadException ex) { rejected=ex.Code=="DUPLICATE"; }
                Assert(rejected,"Duplicate was accepted."); Assert(before==Hash(Path.Combine(f.Destination,"keep.bin")),"Existing file overwritten.");
            });
            await Case("TC_13","Good / duplicate / good queue",async f=>
            {
                string a=f.File("a.bin",16),b=f.File("b.bin",32),c=f.File("c.bin",48);
                await Upload(f,b); await WaitFor(()=>f.Server.ActiveCount==0);
                var q=await new UploadQueue().RunAsync(new List<UploadJob>{new UploadJob(a),new UploadJob(b),new UploadJob(c)},f.Client,null,CancellationToken.None);
                Assert(q.Completed==2 && q.Errors==1 && q.Waiting==0,"Queue stopped on duplicate.");
                Assert(System.IO.File.Exists(Path.Combine(f.Destination,"c.bin")),"Last file missing.");
            });
            await Case("TC_14","Missing source does not stop next file",async f=>
            {
                var q=await new UploadQueue().RunAsync(new List<UploadJob>{new UploadJob(Path.Combine(f.Source,"missing.txt")),new UploadJob(f.File("good.txt",64))},f.Client,null,CancellationToken.None);
                Assert(q.Completed==1 && q.Errors==1 && q.Waiting==0,"Local failure stopped queue.");
            });
            await Case("TC_15","Oversized file rejected",async f=>
            { await Reject(f,new Message{Type="UPLOAD",Name="large.bin",Size=129},"TOO_LARGE"); },max:128);
            await Case("TC_16","Path traversal and reserved names rejected",async f=>
            {
                foreach(string name in new[]{"../escape.txt","..\\escape.txt","C:\\x.txt","CON.txt","NUL","bad:stream","bad.",".udm10-server.lock"})
                { await Reject(f,new Message{Type="UPLOAD",Name=name,Size=0},"INVALID_NAME"); await WaitFor(()=>f.Server.ActiveCount==0); }
            });
            await Case("TC_17","Malformed JSON rejected",async f=>
            {
                using(var c=await Connect(f))
                {
                    byte[] bytes={0,0,0,1,(byte)'{'}; await Wire.WriteBytes(c,bytes,0,bytes.Length,5,CancellationToken.None);
                    Message answer=await Wire.ReadMessage(c,5,CancellationToken.None); Assert(answer.Type=="ERROR" && answer.Code=="BAD_JSON","JSON not rejected.");
                }
            });
            await Case("TC_18","Oversized header length rejected",async f=>
            {
                using(var c=await Connect(f))
                {
                    byte[] prefix={0,1,0,0}; await Wire.WriteBytes(c,prefix,0,4,5,CancellationToken.None);
                    Message answer=await Wire.ReadMessage(c,5,CancellationToken.None); Assert(answer.Code=="BAD_LENGTH","Huge header not rejected.");
                }
            });
            await Case("TC_19","Negative file size rejected",async f=>
            { await Reject(f,new Message{Type="UPLOAD",Name="negative.bin",Size=-1},"BAD_SIZE"); });
            await Case("TC_20","Mid-upload disconnect cleans temporary file",async f=>
            {
                using(var raw=await Raw(f,"broken.bin",10000))
                { byte[] bytes=new byte[100]; await Wire.WriteBytes(raw.Client,bytes,0,bytes.Length,5,CancellationToken.None); }
                await WaitFor(()=>f.Server.ActiveCount==0);
                Assert(!System.IO.File.Exists(Path.Combine(f.Destination,"broken.bin")),"Partial file marked final.");
                Assert(Directory.GetFiles(Path.Combine(f.Destination,".udm10-incoming"),"*.part").Length==0,"Temporary file leaked.");
                await Upload(f,f.File("after.bin",128));
            });
            await Case("TC_21","Stalled upload is timed out",async f=>
            {
                using(var raw=await Raw(f,"stall.bin",10000)) { await WaitFor(()=>f.Server.ActiveCount==0,5000); }
                Assert(!System.IO.File.Exists(Path.Combine(f.Destination,"stall.bin")),"Stalled file finalized.");
                Assert(Directory.GetFiles(Path.Combine(f.Destination,".udm10-incoming"),"*.part").Length==0,"Stalled part leaked.");
            },timeout:1);
            await Case("TC_22","Wrong SHA-256 rejected",async f=>
            {
                using(var raw=await Raw(f,"hash.bin",0))
                {
                    await Wire.WriteMessage(raw.Client,new Message { Type="END",Id=raw.Ready.Id,Sha256=new string('0',64) },5,CancellationToken.None);
                    Message answer=await Wire.ReadMessage(raw.Client,5,CancellationToken.None); Assert(answer.Code=="HASH_MISMATCH","Bad hash accepted.");
                }
                await WaitFor(()=>f.Server.ActiveCount==0);
                Assert(!System.IO.File.Exists(Path.Combine(f.Destination,"hash.bin")),"Hash-mismatched file finalized.");
            });
            await Case("TC_23","Three concurrent clients",async f=>
            {
                var tasks=new List<Task<UploadResult>>();
                for(int i=0;i<3;i++) tasks.Add(Upload(f,f.File("parallel-"+i+".bin",2097152)));
                await Task.WhenAll(tasks);
                for(int i=0;i<3;i++) Assert(Hash(Path.Combine(f.Source,"parallel-"+i+".bin"))==Hash(Path.Combine(f.Destination,"parallel-"+i+".bin")),"Parallel corruption.");
            });
            await Case("TC_24","Session limit returns BUSY",async f=>
            {
                using(var hold=await Raw(f,"hold.bin",10000))
                {
                    bool busy=false; try { await Upload(f,f.File("busy.bin",32)); } catch(UploadException ex) { busy=ex.Code=="BUSY"; }
                    Assert(busy,"Excess client not rejected BUSY."); Assert(f.Server.ActiveCount<=1,"Session limit exceeded.");
                }
            },sessions:1,timeout:5);
            await Case("TC_25","Concurrent filename reservation released after failure",async f=>
            {
                using(var hold=await Raw(f,"same.bin",10000)) { await Reject(f,new Message { Type="UPLOAD",Name="same.bin",Size=0 },"DUPLICATE"); }
                await WaitFor(()=>f.Server.ActiveCount==0); await Upload(f,f.File("same.bin",32));
            });
            await Case("TC_26","Server offline retains unattempted queue items",async f=>
            {
                await f.Server.StopAsync();
                var q=await new UploadQueue().RunAsync(new List<UploadJob>{new UploadJob(f.File("off-a.bin",32)),new UploadJob(f.File("off-b.bin",32))},f.Client,null,CancellationToken.None);
                Assert(q.Errors==1 && q.Waiting==1 && q.ConnectionLost,"Offline queue state incorrect.");
            });
            await Case("TC_27","Stop Server during upload",async f=>
            {
                using(var hold=await Raw(f,"stop.bin",10000)) { await f.Server.StopAsync(); }
                Assert(f.Server.ActiveCount==0,"Active session remained.");
                Assert(!System.IO.File.Exists(Path.Combine(f.Destination,"stop.bin")),"Partial file finalized after stop.");
                Assert(Directory.GetFiles(Path.Combine(f.Destination,".udm10-incoming"),"*.part").Length==0,"Part remained after stop.");
            });
            await Case("TC_28","Crash cleanup removes only owned temporary files",async f=>
            {
                await f.Server.StopAsync(); string tmp=Path.Combine(f.Destination,".udm10-incoming");
                string stale=Path.Combine(tmp,"udm10-"+Guid.NewGuid().ToString("N")+".part"),other=Path.Combine(tmp,"keep-other.part");
                System.IO.File.WriteAllText(stale,"partial"); System.IO.File.WriteAllText(other,"do not delete");
                var restarted=new ServerEngine();
                Exception failure=null;
                try { restarted.Start(new ServerOptions{Folder=f.Destination,Port=0}); Assert(!System.IO.File.Exists(stale),"Stale part retained."); Assert(System.IO.File.Exists(other),"Unowned file deleted."); }
                catch(Exception ex) { failure=ex; }
                await restarted.StopAsync();
                if(failure!=null) ExceptionDispatchInfo.Capture(failure).Throw();
            });
            await Case("TC_29","Second Server cannot use same folder",async f=>
            {
                var second=new ServerEngine(); bool rejected=false;
                try { second.Start(new ServerOptions{Folder=f.Destination,Port=0}); } catch(IOException) { rejected=true; }
                await second.StopAsync();
                Assert(rejected,"Directory lock failed."); await new UploadClientService().ProbeAsync(f.Client,CancellationToken.None);
            });
            await Case("TC_30","Progress is monotonic; Completed follows server ACK",async f=>
            {
                var updates=new List<TransferUpdate>(); await Upload(f,f.File("progress.bin",2097152),u=>updates.Add(u));
                Assert(updates.Count>=2 && updates.Last().State=="Completed" && updates.Last().Percent==100,"Final progress wrong.");
                long previous=0; foreach(var u in updates) { Assert(u.Sent>=previous,"Bytes went backwards."); previous=u.Sent; }
                Assert(updates.Count(u=>u.State=="Completed")==1,"Multiple completion events.");
                detail="Updates="+updates.Count+"; final Completed=100%; final file exists="+System.IO.File.Exists(Path.Combine(f.Destination,"progress.bin"));
            });
            await Case("TC_31","Performance level 1: one client / six 1 MiB files",async f=> { await Measure(f,1,6); },timeout:10);
            await Case("TC_32","Performance level 2: three clients / eighteen 1 MiB files",async f=> { await Measure(f,3,6); },timeout:10);
            await Case("TC_33","Stress: reject overload and recover",async f=>
            {
                var holds=new List<RawSession>();
                try
                {
                    for(int i=0;i<3;i++) holds.Add(await Raw(f,"hold-"+i+".bin",10000));
                    var tasks=new List<Task<bool>>();
                    for(int i=0;i<12;i++) { string path=f.File("overflow-"+i+".bin",64); tasks.Add(ExpectBusy(f,path)); }
                    bool[] rejected=await Task.WhenAll(tasks); Assert(rejected.All(x=>x),"An excess client was not rejected BUSY.");
                    Assert(f.Server.ActiveCount<=3,"Session cap exceeded."); detail="Held sessions=3; rejected BUSY=12/12; server remained available after release.";
                }
                finally { foreach(var h in holds) h.Dispose(); }
                await WaitFor(()=>f.Server.ActiveCount==0); await Upload(f,f.File("recovery.bin",1000));
            },timeout:10);
            await Case("TC_34","Fragmented TCP header and payload",async f=>
            {
                using(var c=await Connect(f))
                {
                    byte[] data=Encoding.UTF8.GetBytes("fragmented TCP is a stream"),header=Wire.Serialize(new Message{Type="UPLOAD",Name="fragments.txt",Size=data.Length});
                    int length=header.Length; byte[] frame=new byte[length+4]; frame[0]=(byte)(length>>24);frame[1]=(byte)(length>>16);frame[2]=(byte)(length>>8);frame[3]=(byte)length; Buffer.BlockCopy(header,0,frame,4,length);
                    for(int i=0;i<frame.Length;i++) await Wire.WriteBytes(c,frame,i,1,5,CancellationToken.None);
                    Message ready=await Wire.ReadMessage(c,5,CancellationToken.None); Assert(ready.Type=="READY","No READY after fragmented header.");
                    for(int i=0;i<data.Length;i++) await Wire.WriteBytes(c,data,i,1,5,CancellationToken.None);
                    string hash; using(var h=SHA256.Create()) hash=FileRules.Hex(h.ComputeHash(data));
                    await Wire.WriteMessage(c,new Message{Type="END",Id=ready.Id,Sha256=hash},5,CancellationToken.None);
                    Message done=await Wire.ReadMessage(c,5,CancellationToken.None); Assert(done.Type=="COMPLETE" && done.Sha256==hash,"Fragmented transfer failed.");
                }
            });
            await Test("TC_35","Lost final ACK is reported as UNKNOWN_RESULT",LostAck);
            await Test("TC_36","Separate Server and Client OS processes",SeparateProcess);
        }
        private static async Task<bool> ExpectBusy(Fixture f,string path)
        { try { await Upload(f,path); return false; } catch(UploadException ex) { return ex.Code=="BUSY"; } }
        private static async Task Measure(Fixture f,int clients,int count)
        {
            var batches=new List<List<UploadJob>>();
            for(int c=0;c<clients;c++)
            {
                var jobs=new List<UploadJob>(); for(int i=0;i<count;i++) jobs.Add(new UploadJob(f.File("load-"+c+"-"+i+".bin",1048576))); batches.Add(jobs);
            }
            var clock=Stopwatch.StartNew();
            QueueResult[] results=await Task.WhenAll(batches.Select(b=>new UploadQueue().RunAsync(b,f.Client,null,CancellationToken.None)));
            clock.Stop(); int errors=results.Sum(r=>r.Errors+r.Waiting); long bytes=clients*(long)count*1048576;
            foreach(var batch in batches) foreach(var job in batch) Assert(Hash(job.Path)==Hash(Path.Combine(f.Destination,Path.GetFileName(job.Path))),"Performance run hash mismatch.");
            Assert(errors==0,"Performance workload had failures.");
            var measure=new Measurement{Id=currentId,Clients=clients,Files=clients*count,Bytes=bytes,Seconds=clock.Elapsed.TotalSeconds,MiBPerSecond=bytes/1048576d/Math.Max(clock.Elapsed.TotalSeconds,0.000001),Errors=errors,WorkingSetBytes=Process.GetCurrentProcess().WorkingSet64};
            run.Measurements.Add(measure);
            detail="Loopback; clients="+clients+"; files="+measure.Files+"; bytes="+bytes+"; seconds="+measure.Seconds.ToString("0.000")+"; MiB/s="+measure.MiBPerSecond.ToString("0.00")+"; errors="+errors+"; processWorkingSet="+measure.WorkingSetBytes+". This is not a LAN benchmark.";
        }
        private static async Task LostAck()
        {
            string root=Path.Combine(Path.GetTempPath(),"UDM10-ACK-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            var listener=new TcpListener(IPAddress.Loopback,0); listener.Start(); int port=((IPEndPoint)listener.LocalEndpoint).Port;
            string src=Path.Combine(root,"ack.txt"); System.IO.File.WriteAllText(src,"test lost acknowledgement");
            Task fake=Task.Run(async delegate
            {
                using(var c=await listener.AcceptTcpClientAsync())
                {
                    await Wire.WriteMessage(c,new Message{Type="WELCOME",Size=100000},5,CancellationToken.None);
                    Message req=await Wire.ReadMessage(c,5,CancellationToken.None); string id=Guid.NewGuid().ToString("N");
                    await Wire.WriteMessage(c,new Message{Type="READY",Id=id},5,CancellationToken.None);
                    long remaining=req.Size.Value; byte[] b=new byte[1024];
                    while(remaining>0) { int n=await Wire.ReadBytes(c,b,(int)Math.Min(b.Length,remaining),5,CancellationToken.None); if(n==0) throw new EndOfStreamException(); remaining-=n; }
                    await Wire.ReadMessage(c,5,CancellationToken.None);
                    // Close without COMPLETE. No claim is made that this fake peer stores a file.
                }
            });
            try
            {
                bool unknown=false;
                try { await new UploadClientService().UploadAsync(new UploadJob(src),new ClientOptions{Port=port,TimeoutSeconds=5},null,CancellationToken.None); }
                catch(UploadException ex) { unknown=ex.Code=="UNKNOWN_RESULT"; }
                Assert(unknown,"Client claimed success or a definitive failure after lost ACK."); await fake;
            }
            finally { listener.Stop(); try { Directory.Delete(root,true); } catch { } }
        }
        private static async Task SeparateProcess()
        {
            // SDK-style .NET 10 builds keep each project in bin/<Config>/<TargetFramework>.
            // Locate the sibling Headless project (or a caller-specified helper), rather
            // than assuming both projects produce executables in the same Build folder.
            string helper=Environment.GetEnvironmentVariable("UDM10_HEADLESS_DLL");
            if(string.IsNullOrWhiteSpace(helper))
            {
                string testBin=AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                string config=Directory.GetParent(testBin).Name;
                string codeFolder=Path.GetFullPath(Path.Combine(testBin,"..","..","..",".."));
                helper=Path.Combine(codeFolder,"Headless","bin",config,"net10.0","Udm10.Headless.dll");
            }
            Assert(System.IO.File.Exists(helper),"Build Udm10.Headless (net10.0) before running TC_36. Expected: "+helper);
            string root=Path.Combine(Path.GetTempPath(),"UDM10-PROC-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            string ready=Path.Combine(root,"ready.txt"),dest=Path.Combine(root,"server"),src=Path.Combine(root,"source.txt");
            System.IO.File.WriteAllText(src,"real separate processes over TCP");
            var captured=new StringBuilder();
            using(var child=new Process())
            {
                child.StartInfo=new ProcessStartInfo("dotnet") { UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true };
                child.StartInfo.ArgumentList.Add(helper);
                child.StartInfo.ArgumentList.Add("serve");
                child.StartInfo.ArgumentList.Add(dest);
                child.StartInfo.ArgumentList.Add("0");
                child.StartInfo.ArgumentList.Add("3");
                child.StartInfo.ArgumentList.Add("5");
                child.StartInfo.ArgumentList.Add(ready);
                child.OutputDataReceived+=delegate(object s,DataReceivedEventArgs e) { if(e.Data!=null) lock(captured) captured.AppendLine(e.Data); };
                child.ErrorDataReceived+=delegate(object s,DataReceivedEventArgs e) { if(e.Data!=null) lock(captured) captured.AppendLine(e.Data); };
                try
                {
                    child.Start(); child.BeginOutputReadLine(); child.BeginErrorReadLine();
                    await WaitFor(()=>System.IO.File.Exists(ready)||child.HasExited,10000);
                    Assert(!child.HasExited,"Child Server exited early.");
                    int port=int.Parse(System.IO.File.ReadAllText(ready));
                    await new UploadClientService().UploadAsync(new UploadJob(src),new ClientOptions{Port=port,TimeoutSeconds=5},null,CancellationToken.None);
                    Assert(Hash(src)==Hash(Path.Combine(dest,"source.txt")),"Separate-process hash mismatch.");
                    detail="Client/test PID="+Process.GetCurrentProcess().Id+"; Server PID="+child.Id+"; TCP port="+port+"; source/destination SHA-256 matched.";
                }
                finally
                {
                    try { if(!child.HasExited) { child.StandardInput.WriteLine("STOP"); child.StandardInput.Flush(); if(!child.WaitForExit(7000)) child.Kill(); child.WaitForExit(); } } catch { }
                    lock(captured) System.IO.File.WriteAllText(Path.Combine(Output,"TC_36-process.log"),captured.ToString());
                    try { Directory.Delete(root,true); } catch { }
                }
            }
        }
        private static void Save()
        {
            using(var stream=System.IO.File.Create(Path.Combine(Output,"results.json"))) new DataContractJsonSerializer(typeof(TestRun)).WriteObject(stream,run);
            var csv=new StringBuilder("Id,Name,Stage,Seconds,Actual\r\n");
            foreach(var r in run.Records) csv.AppendLine(Csv(r.Id)+","+Csv(r.Name)+","+Csv(r.Stage)+","+r.Seconds.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)+","+Csv(r.Actual));
            System.IO.File.WriteAllText(Path.Combine(Output,"results.csv"),csv.ToString(),new UTF8Encoding(true));
            System.IO.File.WriteAllText(Path.Combine(Output,"environment.txt"),run.StartedUtc+Environment.NewLine+run.Environment+Environment.NewLine+run.Scope,Encoding.UTF8);
        }
        private static string Csv(string s) { return "\""+(s??"").Replace("\"","\"\"")+"\""; }
    }
}
