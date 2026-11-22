using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Udm10.Shared
{
    // TV3: file streaming and protocol; this class has NO dependency on Windows Forms.
    public sealed class UploadClientService
    {
        private static async Task Check(TcpClient client,Message m,string expected,int timeoutSeconds)
        {
            if(m.Type=="ERROR")
            {
                // Wait for the server to release the old session before the next queued file.
                try { await Wire.ReadBytes(client,new byte[1],1,Math.Min(timeoutSeconds,2),CancellationToken.None).ConfigureAwait(false); } catch { }
                throw new UploadException(m.Code??"SERVER_ERROR",m.Text??"Server rejected the request.",false);
            }
            if(m.Type!=expected) throw new ProtocolException("BAD_PROTOCOL", "Expected "+expected+", received "+m.Type+".");
        }
        private static async Task<Message> Connect(TcpClient client,ClientOptions options,CancellationToken token)
        {
            await Wire.Timed(client.ConnectAsync(options.Host,options.Port),options.TimeoutSeconds*1000,client.Close,token).ConfigureAwait(false);
            Message greeting=await Wire.ReadMessage(client,options.TimeoutSeconds,token).ConfigureAwait(false);
            await Check(client,greeting,"WELCOME",options.TimeoutSeconds).ConfigureAwait(false);
            return greeting;
        }
        public async Task ProbeAsync(ClientOptions options,CancellationToken token)
        {
            options.Validate();
            using(var client=new TcpClient(AddressFamily.InterNetwork))
            using(token.Register(client.Close))
            {
                await Connect(client,options,token).ConfigureAwait(false);
                await Wire.WriteMessage(client,Message.Of("PING"),options.TimeoutSeconds,token).ConfigureAwait(false);
                await Check(client,await Wire.ReadMessage(client,options.TimeoutSeconds,token).ConfigureAwait(false),"PONG",options.TimeoutSeconds).ConfigureAwait(false);
            }
        }
        public async Task<UploadResult> UploadAsync(UploadJob job,ClientOptions options,Action<TransferUpdate> report,CancellationToken token)
        {
            options.Validate();
            FileStream file;
            string name;
            try
            {
                name=FileRules.ValidateName(Path.GetFileName(job.Path));
                file=new FileStream(job.Path,FileMode.Open,FileAccess.Read,FileShare.Read,Wire.BufferSize,true);
            }
            catch(Exception ex)
            { throw new UploadException("LOCAL_FILE",ex.Message,false,ex); }
            bool endSent=false;
            using(file)
            using(var client=new TcpClient(AddressFamily.InterNetwork))
            using(token.Register(client.Close))
            using(var hash=SHA256.Create())
            {
                long size=file.Length, sent=0;
                var clock=new Stopwatch();
                try
                {
                    Message hello=await Connect(client,options,token).ConfigureAwait(false);
                    if(hello.Size.HasValue && size>hello.Size.Value) throw new UploadException("TOO_LARGE","File exceeds server size limit.",false);
                    await Wire.WriteMessage(client,new Message { Type="UPLOAD",Name=name,Size=size },options.TimeoutSeconds,token).ConfigureAwait(false);
                    Message ready=await Wire.ReadMessage(client,options.TimeoutSeconds,token).ConfigureAwait(false);
                    await Check(client,ready,"READY",options.TimeoutSeconds).ConfigureAwait(false);
                    if(string.IsNullOrEmpty(ready.Id)) throw new ProtocolException("BAD_PROTOCOL","Missing transfer identifier.");
                    clock.Start();
                    Publish(report,job,"Uploading",0,size,0,"Sending data");
                    byte[] buffer=new byte[Wire.BufferSize];
                    long lastReport=0;
                    while(sent<size)
                    {
                        token.ThrowIfCancellationRequested();
                        int count;
                        try { count=await file.ReadAsync(buffer,0,(int)Math.Min(buffer.Length,size-sent),token).ConfigureAwait(false); }
                        catch(OperationCanceledException) { throw; }
                        catch(Exception ex) { throw new UploadException("LOCAL_READ",ex.Message,false,ex); }
                        if(count==0) throw new UploadException("LOCAL_CHANGED","Source file ended before its declared size.",false);
                        hash.TransformBlock(buffer,0,count,buffer,0);
                        await Wire.WriteBytes(client,buffer,0,count,options.TimeoutSeconds,token).ConfigureAwait(false);
                        sent+=count;
                        if(clock.ElapsedMilliseconds-lastReport>=100 || sent==size)
                        {
                            lastReport=clock.ElapsedMilliseconds;
                            Publish(report,job,"Uploading",sent,size,sent/Math.Max(clock.Elapsed.TotalSeconds,0.001),"Sending data");
                        }
                    }
                    hash.TransformFinalBlock(new byte[0],0,0);
                    string digest=FileRules.Hex(hash.Hash);
                    // Byte progress can reach 100 before the application-level COMPLETE acknowledgement.
                    Publish(report,job,"Uploading",sent,size,sent/Math.Max(clock.Elapsed.TotalSeconds,0.001),"Waiting for server verification");
                    endSent=true; // A failed END write may still have reached the server: be conservative.
                    await Wire.WriteMessage(client,new Message { Type="END",Id=ready.Id,Sha256=digest },options.TimeoutSeconds,token).ConfigureAwait(false);
                    Message complete=await Wire.ReadMessage(client,options.TimeoutSeconds,token).ConfigureAwait(false);
                    await Check(client,complete,"COMPLETE",options.TimeoutSeconds).ConfigureAwait(false);
                    if(complete.Id!=ready.Id || complete.Size!=size || !string.Equals(complete.Sha256,digest,StringComparison.OrdinalIgnoreCase))
                        throw new ProtocolException("BAD_ACK","Server acknowledgement does not match this file.");
                    // A valid COMPLETE is definitive even if TCP close is lost. Waiting for EOF
                    // also avoids racing the previous server session slot in a fast local queue.
                    try { await Wire.ReadBytes(client,new byte[1],1,Math.Min(options.TimeoutSeconds,2),CancellationToken.None).ConfigureAwait(false); } catch { }
                    clock.Stop();
                    Publish(report,job,"Completed",sent,size,sent/Math.Max(clock.Elapsed.TotalSeconds,0.001),"SHA-256 verified; stored as "+complete.Name);
                    return new UploadResult { Name=complete.Name,Bytes=size,Sha256=digest,Seconds=clock.Elapsed.TotalSeconds };
                }
                catch(UploadException) { throw; }
                catch(Exception ex)
                {
                    if(endSent)
                        throw new UploadException("UNKNOWN_RESULT","Final acknowledgement was not confirmed. The file may already be stored; inspect Server before retrying. "+ex.Message,true,ex);
                    token.ThrowIfCancellationRequested();
                    if(ex is ProtocolException) throw new UploadException("BAD_PROTOCOL",ex.Message,false,ex);
                    throw new UploadException(ex is TimeoutException ? "TIMEOUT" : "CONNECTION_LOST",ex.Message,true,ex);
                }
            }
        }
        internal static void Publish(Action<TransferUpdate> report,UploadJob job,string state,long sent,long total,double speed,string text)
        {
            if(report!=null) report(new TransferUpdate { JobId=job.Id,State=state,Sent=sent,Total=total,BytesPerSecond=speed,Detail=text });
        }
    }
}