using System;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Class)]
    public class StoreToDiskAttribute:Attribute{
        public string Key{ get; }
        public string[] Properties{ get; }

        public StoreToDiskAttribute(string key,params string[] properties){
            Key = key;
            Properties = properties;
        }
    }
}