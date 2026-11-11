using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Udm10.Shared
{
    // TV1 + TV3 + TV4. TCP has no message boundaries: read EXACTLY the prefix/body.
    public static class Wire
    {
        public const int MaxHeaderBytes=16384;
        public const int BufferSize=65536;
        public static byte[] Serialize(Message message)
        {
            using(var memory=new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(Message)).WriteObject(memory,message);
                return memory.ToArray();
            }
        }
        public static Message Deserialize(byte[] bytes)
        {
            try
            {
                using(var memory=new MemoryStream(bytes))
                {
                    var serializer=new DataContractJsonSerializer(typeof(Message),new DataContractJsonSerializerSettings { MaxItemsInObjectGraph=64 });
                    var value=(Message)serializer.ReadObject(memory);
                    if(value==null || value.Version!=1 || string.IsNullOrEmpty(value.Type))
                        throw new ProtocolException("BAD_PROTOCOL", "Protocol version/type is invalid.");
                    return value;
                }
            }
            catch(ProtocolException) { throw; }
            catch(Exception ex) { throw new ProtocolException("BAD_JSON", "Invalid control message: "+ex.GetType().Name); }
        }
        public static async Task<T> Timed<T>(Task<T> work,int milliseconds,Action abort,CancellationToken token)
        {
            using(var delayCancellation=CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                Task delay=Task.Delay(milliseconds,delayCancellation.Token);
                if(await Task.WhenAny(work,delay).ConfigureAwait(false)==work)
                {
                    delayCancellation.Cancel();
                    return await work.ConfigureAwait(false);
                }
                abort();
                // Observe any late exception after closing the socket. Never leave unobserved faults.
                Observe(work);
                token.ThrowIfCancellationRequested();
                throw new TimeoutException("Network operation timed out.");
            }
        }
        public static async Task Timed(Task work,int milliseconds,Action abort,CancellationToken token)
        {
            await Timed(Wrap(work),milliseconds,abort,token).ConfigureAwait(false);
        }
        private static async Task<bool> Wrap(Task work) { await work.ConfigureAwait(false); return true; }
        private static void Observe(Task work)
        { work.ContinueWith(t => { var ignored=t.Exception; },CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default); }
        public static async Task ReadExactCore(Stream stream,byte[] bytes,int offset,int count,CancellationToken token)
        {
            while(count>0)
            {
                int read=await stream.ReadAsync(bytes,offset,count,token).ConfigureAwait(false);
                if(read==0) throw new EndOfStreamException("Peer disconnected before all bytes were received.");
                offset+=read; count-=read;
            }
        }
        public static async Task<Message> ReadMessage(TcpClient client,int timeoutSeconds,CancellationToken token)
        { return await Timed(ReadMessageCore(client.GetStream(),token),timeoutSeconds*1000,client.Close,token).ConfigureAwait(false); }
        private static async Task<Message> ReadMessageCore(Stream stream,CancellationToken token)
        {
            byte[] prefix=new byte[4];
            await ReadExactCore(stream,prefix,0,4,token).ConfigureAwait(false);
            int length=(prefix[0]<<24)|(prefix[1]<<16)|(prefix[2]<<8)|prefix[3];
            if(length<1 || length>MaxHeaderBytes) throw new ProtocolException("BAD_LENGTH", "Control frame must be 1..16384 bytes.");
            byte[] body=new byte[length];
            await ReadExactCore(stream,body,0,length,token).ConfigureAwait(false);
            return Deserialize(body);
        }
        public static async Task WriteMessage(TcpClient client,Message message,int timeoutSeconds,CancellationToken token)
        {
            byte[] body=Serialize(message);
            if(body.Length>MaxHeaderBytes) throw new ProtocolException("BAD_LENGTH", "Control frame too large.");
            byte[] frame=new byte[body.Length+4]; int n=body.Length;
            frame[0]=(byte)(n>>24); frame[1]=(byte)(n>>16); frame[2]=(byte)(n>>8); frame[3]=(byte)n;
            Buffer.BlockCopy(body,0,frame,4,n);
            await WriteBytes(client,frame,0,frame.Length,timeoutSeconds,token).ConfigureAwait(false);
        }
        public static async Task WriteBytes(TcpClient client,byte[] bytes,int offset,int count,int timeoutSeconds,CancellationToken token)
        { await Timed(client.GetStream().WriteAsync(bytes,offset,count,token),timeoutSeconds*1000,client.Close,token).ConfigureAwait(false); }
        public static async Task<int> ReadBytes(TcpClient client,byte[] bytes,int count,int timeoutSeconds,CancellationToken token)
        { return await Timed(client.GetStream().ReadAsync(bytes,0,count,token),timeoutSeconds*1000,client.Close,token).ConfigureAwait(false); }
    }
}
