using System;
using System.Security.Cryptography;

namespace Xpand.XAF.Modules.StoreToDisk{
    [AttributeUsage(AttributeTargets.Class)]
    public class StoreToDiskAttribute:Attribute{
        public string Key{ get; }
        public string[] Properties{ get; }

        public StoreToDiskAttribute(string key,params string[] properties){
            Key = key;
            Properties = properties;
        }
        public StoreToDiskAttribute(string key,DataProtectionScope scope,params string[] properties){
            Key = key;
            Properties = properties;
            Protection=scope;
        }

        public DataProtectionScope? Protection { get; set; }
    }
}