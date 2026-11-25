using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Udm10.Shared;

namespace Udm10.ServerCore
{
    public sealed class ServerEvent
    {
        public string Id { get; set; }
        public string Remote { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public string Detail { get; set; }
        public long Received { get; set; }
        public long Total { get; set; }
        public double BytesPerSecond { get; set; }
    }
    // TV4: network/storage engine. All UI and logging observers are isolated from connections.
    public sealed class ServerEngine
    {
        private readonly object gate=new object();
        private readonly object logGate=new object();
        private readonly object stopGate=new object();
        private readonly HashSet<string> names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string,Task> sessions=new Dictionary<string,Task>();
        private TcpListener listener;
        private CancellationTokenSource shutdown;
        private SemaphoreSlim slots;
        private FileStream folderLock;
        private StreamWriter logWriter;
        private Task acceptTask;
        private Task stopTask;
        private ServerOptions options;
        private string incoming;
        public event Action<string> Log;
        public event Action<ServerEvent> Changed;
        public int BoundPort { get; private set; }
        public string LogPath { get; private set; }
        public bool IsRunning { get { return shutdown!=null && !shutdown.IsCancellationRequested; } }
        public int ActiveCount { get { lock(gate) return sessions.Count; } }

        public void Start(ServerOptions config)
        {
            if(shutdown!=null) throw new InvalidOperationException("This server instance was already started. Create a new instance to restart.");
            if(config==null) throw new ArgumentNullException("config");
            config.Validate();
            options=new ServerOptions { Address=config.Address,Port=config.Port,Folder=config.Folder,MaxSessions=config.MaxSessions,TimeoutSeconds=config.TimeoutSeconds,MaxFileBytes=config.MaxFileBytes };
            Directory.CreateDirectory(options.Folder);
            if((File.GetAttributes(options.Folder)&FileAttributes.ReparsePoint)!=0)
                throw new IOException("Choose a normal local folder, not a symlink or junction.");
            try
            {
                // Prevent two Server processes from writing the same directory.
                folderLock=new FileStream(Path.Combine(options.Folder,".udm10-server.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
                incoming=Path.Combine(options.Folder,".udm10-incoming");
                Directory.CreateDirectory(incoming);
                if((File.GetAttributes(incoming)&FileAttributes.ReparsePoint)!=0) throw new IOException("Temporary folder cannot be a symlink/junction.");
                string logFolder=Path.Combine(options.Folder,".udm10-logs");
                Directory.CreateDirectory(logFolder);
                if((File.GetAttributes(logFolder)&FileAttributes.ReparsePoint)!=0) throw new IOException("Log folder cannot be a symlink/junction.");
                LogPath=Path.Combine(logFolder,DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8)+".log");
                logWriter=new StreamWriter(new FileStream(LogPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read),new UTF8Encoding(false));
                logWriter.AutoFlush=true;
                // Only our GUID-named .part files are cleanup candidates after a prior crash.
                foreach(string path in Directory.GetFiles(incoming,"udm10-*.part"))
                {
                    string stem=Path.GetFileNameWithoutExtension(path); Guid id;
                    if(stem.Length==38 && Guid.TryParseExact(stem.Substring(6),"N",out id))
                    {
                        try { File.Delete(path); WriteLog("STALE_PART_REMOVED "+Path.GetFileName(path)); }
                        catch(Exception ex) { WriteLog("STALE_PART_WARNING "+ex.Message); }
                    }
                }
                listener=new TcpListener(IPAddress.Parse(options.Address),options.Port);
                listener.Start(32);
                BoundPort=((IPEndPoint)listener.LocalEndpoint).Port;
                shutdown=new CancellationTokenSource(); slots=new SemaphoreSlim(options.MaxSessions,options.MaxSessions);
                WriteLog("SERVER_START "+options.Address+":"+BoundPort+" sessions="+options.MaxSessions+" maxBytes="+options.MaxFileBytes+" timeout="+options.TimeoutSeconds);
                acceptTask=Task.Run(() => AcceptLoop(shutdown.Token));
            }
            catch
            {
                if(listener!=null) listener.Stop();
                if(logWriter!=null) { logWriter.Dispose(); logWriter=null; }
                if(folderLock!=null) { folderLock.Dispose(); folderLock=null; }
                throw;
            }
        }
        private async Task AcceptLoop(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                TcpClient client;
                try { client=await listener.AcceptTcpClientAsync().ConfigureAwait(false); }
                catch(ObjectDisposedException) { break; }
                catch(SocketException ex)
                {
                    if(token.IsCancellationRequested) break;
                    WriteLog("ACCEPT_ERROR "+ex.Message);
                    // Error backoff on the worker thread (C# 5 cannot await in catch).
                    Thread.Sleep(100);
                    continue;
                }
                if(token.IsCancellationRequested) { client.Close(); break; }
                client.NoDelay=true;
                if(!slots.Wait(0))
                {
                    WriteLog("BUSY "+client.Client.RemoteEndPoint);
                    try { await Wire.WriteMessage(client,Message.Error("BUSY","Server session limit reached. Retry this file later."),1,token).ConfigureAwait(false); }
                    catch { /* Closing a busy connection is an acceptable bounded rejection. */ }
                    finally { client.Close(); }
                    continue;
                }
                string id=Guid.NewGuid().ToString("N");
                lock(gate)
                {
                    // The same gate protects insertion and final removal from this dictionary.
                    sessions[id]=Task.Run(() => HandleClient(id,client,token));
                }
            }
        }
        private async Task HandleClient(string id,TcpClient client,CancellationToken token)
        {
            string remote="unknown", name=null, temp=null;
            bool reserved=false, committed=false;
            long expected=0, received=0;
            try { remote=Convert.ToString(client.Client.RemoteEndPoint); } catch { }
            WriteLog("CONNECT id="+id+" peer="+remote);
            Emit(id,remote,"","Connected",0,0,0,"");
            using(client)
            using(token.Register(client.Close))
            {
                Message pendingError=null;
                try
                {
                  try
                  {
                    await Wire.WriteMessage(client,new Message { Type="WELCOME",Size=options.MaxFileBytes,Text="UDM10 v1; one file per connection" },options.TimeoutSeconds,token).ConfigureAwait(false);
                    Message request=await Wire.ReadMessage(client,options.TimeoutSeconds,token).ConfigureAwait(false);
                    if(request.Type=="PING")
                    {
                        await Wire.WriteMessage(client,Message.Of("PONG"),options.TimeoutSeconds,token).ConfigureAwait(false);
                        Emit(id,remote,"","Completed",0,0,0,"Connectivity probe");
                        return;
                    }
                    if(request.Type!="UPLOAD") throw new ProtocolException("BAD_PROTOCOL","Expected UPLOAD or PING.");
                    name=FileRules.ValidateName(request.Name);
                    if(!request.Size.HasValue || request.Size.Value<0) throw new ProtocolException("BAD_SIZE","File size must be a nonnegative Int64.");
                    expected=request.Size.Value;
                    if(expected>options.MaxFileBytes) throw new ProtocolException("TOO_LARGE","File exceeds configured maximum.");
                    string destination=Path.Combine(options.Folder,name);
                    // ASCII-case-insensitive reservation also prevents concurrent duplicate writers.
                    lock(gate)
                    {
                        if(names.Contains(name) || File.Exists(destination) || Directory.Exists(destination))
                            throw new ProtocolException("DUPLICATE","File name already exists or is being uploaded. Existing data is preserved.");
                        names.Add(name); reserved=true;
                    }
                    temp=Path.Combine(incoming,"udm10-"+id+".part");
                    string digest;
                    var clock=Stopwatch.StartNew();
                    using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,Wire.BufferSize,true))
                    using(var hash=SHA256.Create())
                    {
                        WriteLog("UPLOAD_START id="+id+" name="+name+" bytes="+expected);
                        await Wire.WriteMessage(client,new Message { Type="READY",Id=id,Name=name },options.TimeoutSeconds,token).ConfigureAwait(false);
                        Emit(id,remote,name,"Uploading",0,expected,0,"");
                        byte[] buffer=new byte[Wire.BufferSize]; long lastReport=0;
                        while(received<expected)
                        {
                            int count=await Wire.ReadBytes(client,buffer,(int)Math.Min(buffer.Length,expected-received),options.TimeoutSeconds,token).ConfigureAwait(false);
                            if(count==0) throw new EndOfStreamException("Client disconnected before declared file size.");
                            hash.TransformBlock(buffer,0,count,buffer,0);
                            await file.WriteAsync(buffer,0,count,token).ConfigureAwait(false);
                            received+=count;
                            if(clock.ElapsedMilliseconds-lastReport>=150 || received==expected)
                            {
                                lastReport=clock.ElapsedMilliseconds;
                                Emit(id,remote,name,"Uploading",received,expected,received/Math.Max(clock.Elapsed.TotalSeconds,0.001),"");
                            }
                        }
                        Message end=await Wire.ReadMessage(client,options.TimeoutSeconds,token).ConfigureAwait(false);
                        if(end.Type!="END" || end.Id!=id || !FileRules.ValidHash(end.Sha256)) throw new ProtocolException("BAD_END","Invalid END / transfer ID / SHA-256.");
                        hash.TransformFinalBlock(new byte[0],0,0); digest=FileRules.Hex(hash.Hash);
                        if(!string.Equals(digest,end.Sha256,StringComparison.OrdinalIgnoreCase)) throw new ProtocolException("HASH_MISMATCH","Received bytes do not match sender checksum.");
                        await file.FlushAsync(token).ConfigureAwait(false);
                    }
                    token.ThrowIfCancellationRequested();
                    // Same filesystem rename; File.Move never overwrites an existing destination.
                    File.Move(temp,destination); committed=true;
                    WriteLog("UPLOAD_COMMIT id="+id+" name="+name+" bytes="+received+" sha256="+digest);
                    Emit(id,remote,name,"Completed",received,expected,received/Math.Max(clock.Elapsed.TotalSeconds,0.001),digest);
                    await Wire.WriteMessage(client,new Message { Type="COMPLETE",Id=id,Name=name,Size=received,Sha256=digest },options.TimeoutSeconds,token).ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                    string code=ex is ProtocolException ? ((ProtocolException)ex).Code : ex is TimeoutException ? "TIMEOUT" : token.IsCancellationRequested ? "SERVER_STOP" : ex is EndOfStreamException ? "INCOMPLETE" : ex is IOException ? "IO_ERROR" : "TRANSFER_ERROR";
                    WriteLog((committed ? "ACK_FAILED " : "UPLOAD_ERROR ")+"id="+id+" code="+code+" bytes="+received+"/"+expected+" detail="+ex.Message);
                    if(!committed) Emit(id,remote,name??"","Error",received,expected,0,code+": "+ex.Message);
                    // Do not leak the server's local paths in remote errors.
                    if(!token.IsCancellationRequested && !committed)
                    {
                        string safeText=ex is ProtocolException ? ex.Message : "Transfer failed ("+code+"). See Server log for details.";
                        pendingError=Message.Error(code,safeText);
                    }
                  }
                  if(pendingError!=null)
                  {
                      try { await Wire.WriteMessage(client,pendingError,2,CancellationToken.None).ConfigureAwait(false); } catch { }
                  }
                }
                finally
                {
                    if(temp!=null && !committed)
                    {
                        try { if(File.Exists(temp)) File.Delete(temp); }
                        catch(Exception ex) { WriteLog("PART_CLEANUP_WARNING id="+id+" "+ex.Message); }
                    }
                    lock(gate) { if(reserved) names.Remove(name); sessions.Remove(id); }
                    slots.Release();
                    WriteLog("DISCONNECT id="+id+" peer="+remote);
                }
            }
        }
        public Task StopAsync()
        {
            lock(stopGate)
            {
                if(shutdown==null) return Task.CompletedTask;
                if(stopTask==null) stopTask=StopCoreAsync();
                return stopTask;
            }
        }
        private async Task StopCoreAsync()
        {
            try
            {
                shutdown.Cancel(); listener.Stop();
                if(acceptTask!=null) { try { await acceptTask.ConfigureAwait(false); } catch(OperationCanceledException) { } }
                Task[] pending; lock(gate) pending=sessions.Values.ToArray();
                await Task.WhenAll(pending).ConfigureAwait(false);
                WriteLog("SERVER_STOP");
            }
            finally
            {
                lock(logGate) { if(logWriter!=null) { logWriter.Dispose(); logWriter=null; } }
                if(folderLock!=null) { folderLock.Dispose(); folderLock=null; }
                // Keep shutdown as a stopped marker. Create a new instance to restart.
            }
        }
        private void Emit(string id,string remote,string name,string state,long received,long total,double speed,string detail)
        {
            var handler=Changed;
            if(handler!=null)
            {
                foreach(Action<ServerEvent> observer in handler.GetInvocationList())
                {
                    try { observer(new ServerEvent { Id=id,Remote=remote,Name=name,State=state,Received=received,Total=total,BytesPerSecond=speed,Detail=detail }); }
                    catch(Exception ex) { WriteLog("OBSERVER_ERROR "+ex.GetType().Name); }
                }
            }
        }
        private void WriteLog(string text)
        {
            string line=DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")+" | "+text.Replace("\r"," ").Replace("\n"," ");
            lock(logGate)
            {
                try { if(logWriter!=null) logWriter.WriteLine(line); }
                catch(IOException) { line+=" [LOG_FILE_WRITE_FAILED]"; }
                catch(ObjectDisposedException) { }
            }
            var handler=Log;
            if(handler!=null)
                foreach(Action<string> observer in handler.GetInvocationList())
                { try { observer(line); } catch { /* One UI observer cannot suppress other logs. */ } }
        }
    }
}
