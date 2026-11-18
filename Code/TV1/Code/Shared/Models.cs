using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;

namespace Udm10.Shared
{
    // TV1: the shared wire contract. A new connection carries ONE upload or ONE ping.
    [DataContract]
    public sealed class Message
    {
        [DataMember(Name="version", IsRequired=true)] public int Version { get; set; }
        [DataMember(Name="type", IsRequired=true)] public string Type { get; set; }
        [DataMember(Name="name", EmitDefaultValue=false)] public string Name { get; set; }
        [DataMember(Name="size", EmitDefaultValue=false)] public long? Size { get; set; }
        [DataMember(Name="id", EmitDefaultValue=false)] public string Id { get; set; }
        [DataMember(Name="sha256", EmitDefaultValue=false)] public string Sha256 { get; set; }
        [DataMember(Name="code", EmitDefaultValue=false)] public string Code { get; set; }
        [DataMember(Name="message", EmitDefaultValue=false)] public string Text { get; set; }
        public Message() { Version = 1; }
        public static Message Of(string type) { return new Message { Type=type }; }
        public static Message Error(string code, string text)
        { return new Message { Type="ERROR", Code=code, Text=text }; }
    }

    public sealed class ClientOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int TimeoutSeconds { get; set; }
        public ClientOptions() { Host="127.0.0.1"; Port=5000; TimeoutSeconds=30; }
        public void Validate()
        {
            IPAddress ip;
            if (!IPAddress.TryParse(Host, out ip) || ip.AddressFamily!=AddressFamily.InterNetwork || ip.Equals(IPAddress.Any))
                throw new ArgumentException("Enter a valid IPv4 server address (not 0.0.0.0).");
            if(Port<1 || Port>65535) throw new ArgumentException("Port must be between 1 and 65535.");
            if(TimeoutSeconds<1 || TimeoutSeconds>600) throw new ArgumentException("Timeout must be 1..600 seconds.");
        }
    }
    public sealed class ServerOptions
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string Folder { get; set; }
        public int MaxSessions { get; set; }
        public int TimeoutSeconds { get; set; }
        public long MaxFileBytes { get; set; }
        public ServerOptions()
        {
            Address="127.0.0.1"; Port=5000; MaxSessions=3; TimeoutSeconds=30;
            MaxFileBytes=1024L*1024*1024;
            Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UDM10_Uploads");
        }
        public void Validate()
        {
            IPAddress ip;
            if(!IPAddress.TryParse(Address,out ip) || ip.AddressFamily!=AddressFamily.InterNetwork)
                throw new ArgumentException("Listen address must be IPv4.");
            // Port 0 is used only by the test harness; the GUI accepts 1..65535.
            if(Port<0 || Port>65535) throw new ArgumentException("Invalid port.");
            if(MaxSessions<1 || MaxSessions>16) throw new ArgumentException("Session limit must be 1..16.");
            if(TimeoutSeconds<1 || TimeoutSeconds>600) throw new ArgumentException("Timeout must be 1..600 seconds.");
            if(MaxFileBytes<0 || MaxFileBytes>10L*1024*1024*1024) throw new ArgumentException("Maximum file size must be 0..10 GiB.");
            if(string.IsNullOrWhiteSpace(Folder)) throw new ArgumentException("Choose a storage folder.");
            Folder=Path.GetFullPath(Folder);
        }
    }
    public static class FileRules
    {
        public static string ValidateName(string name)
        {
            if(string.IsNullOrWhiteSpace(name) || name.Length>180) throw new ProtocolException("INVALID_NAME", "File name must contain 1..180 characters.");
            if(name=="." || name==".." || name.EndsWith(".") || name.EndsWith(" ") || name.StartsWith(".udm10",StringComparison.OrdinalIgnoreCase))
                throw new ProtocolException("INVALID_NAME", "Reserved or ambiguous file name.");
            const string forbidden="<>:\"/\\|?*";
            foreach(char c in name)
                if(char.IsControl(c) || forbidden.IndexOf(c)>=0) throw new ProtocolException("INVALID_NAME", "Paths and special characters are not accepted.");
            string stem=name.Split('.')[0].ToUpperInvariant();
            if(stem=="CON" || stem=="PRN" || stem=="AUX" || stem=="NUL" || stem=="CLOCK$")
                throw new ProtocolException("INVALID_NAME", "Reserved Windows device name.");
            if(stem.Length==4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && stem[3]>='1' && stem[3]<='9')
                throw new ProtocolException("INVALID_NAME", "Reserved Windows device name.");
            return name.Normalize(NormalizationForm.FormC);
        }
        public static bool ValidHash(string text)
        {
            if(text==null || text.Length!=64) return false;
            foreach(char c in text) if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')||(c>='A'&&c<='F'))) return false;
            return true;
        }
        public static string Hex(byte[] bytes) { return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant(); }
        public static string Bytes(long bytes)
        {
            if(bytes>=1024L*1024*1024) return (bytes/(1024d*1024*1024)).ToString("0.00")+" GiB";
            if(bytes>=1024*1024) return (bytes/(1024d*1024)).ToString("0.00")+" MiB";
            if(bytes>=1024) return (bytes/1024d).ToString("0.0")+" KiB";
            return bytes+" B";
        }
    }
    public sealed class ProtocolException : Exception
    {
        public string Code { get; private set; }
        public ProtocolException(string code,string text) : base(text) { Code=code; }
    }
    public sealed class UploadException : Exception
    {
        public string Code { get; private set; }
        public bool ConnectionUnavailable { get; private set; }
        public UploadException(string code,string text,bool unavailable,Exception inner=null) : base(text,inner)
        { Code=code; ConnectionUnavailable=unavailable; }
    }
    public sealed class UploadJob
    {
        public string Id { get; private set; }
        public string Path { get; private set; }
        public UploadJob(string path) { Id=Guid.NewGuid().ToString("N"); Path=path; }
    }
    public sealed class TransferUpdate
    {
        public string JobId { get; set; }
        public string State { get; set; }
        public string Detail { get; set; }
        public long Sent { get; set; }
        public long Total { get; set; }
        public double BytesPerSecond { get; set; }
        public int Percent { get { return State=="Completed" ? 100 : (Total<=0 ? 0 : (int)Math.Min(100,Sent*100d/Total)); } }
    }
    public sealed class UploadResult
    {
        public string Name { get; set; }
        public string Sha256 { get; set; }
        public long Bytes { get; set; }
        public double Seconds { get; set; }
    }
    public sealed class QueueResult
    {
        public int Completed { get; set; }
        public int Errors { get; set; }
        public int Waiting { get; set; }
        public bool ConnectionLost { get; set; }
    }
}
