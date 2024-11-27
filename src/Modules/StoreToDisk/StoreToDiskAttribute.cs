using System;
using System.Security.Cryptography;

namespace Xpand.XAF.Modules.StoreToDisk{
    [AttributeUsage(AttributeTargets.Class)]
    public class StoreToDiskAttribute:Attribute{
        public string Key{ get; }
        public string Criteria{ get; set; }
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

        public StoreToDiskAttribute(){
        }

        public DataProtectionScope? Protection { get; set; }

        public bool AutoCreate { get; set; }

        public bool Map { get; set; }
    }
}