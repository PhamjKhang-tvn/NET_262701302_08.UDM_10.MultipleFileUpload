using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Udm10.Shared
{
    [DataContract]
    public sealed class AppSettings
    {
        [DataMember] public string Host { get; set; }
        [DataMember] public int Port { get; set; }
        [DataMember] public int Timeout { get; set; }
        [DataMember] public string Folder { get; set; }
        [DataMember] public int MaxSessions { get; set; }
        [DataMember] public int MaxMiB { get; set; }
        public AppSettings()
        { Host="127.0.0.1"; Port=5000; Timeout=30; MaxSessions=3; MaxMiB=1024; Folder=new ServerOptions().Folder; }
        public static string SettingsFolder { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"UDM10"); } }
        public static AppSettings Load(string role)
        {
            try
            {
                using(var input=File.OpenRead(Path.Combine(SettingsFolder,role+".json")))
                    return (AppSettings)new DataContractJsonSerializer(typeof(AppSettings)).ReadObject(input) ?? new AppSettings();
            }
            catch { return new AppSettings(); }
        }
        public void Save(string role)
        {
            Directory.CreateDirectory(SettingsFolder);
            string target=Path.Combine(SettingsFolder,role+".json");
            string temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var output=File.Create(temp)) new DataContractJsonSerializer(typeof(AppSettings)).WriteObject(output,this);
                if(File.Exists(target)) File.Replace(temp,target,null); else File.Move(temp,target);
            }
            finally { if(File.Exists(temp)) File.Delete(temp); }
        }
    }
}
