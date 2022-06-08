using System;

namespace Xpand.Extensions.XAF.Attributes{
    public interface IReloadWhenChange {
        Action<string> WhenPropertyChanged { get; }
    }
    public class ReloadWhenChangeAttribute:Attribute {
        public Type[] Types{ get; }
        public ReloadWhenChangeAttribute(params Type[] types) => Types = types;

        public string ObjectPropertyChangeMethodName{ get; }

        public ReloadWhenChangeAttribute(string objectPropertyChangeMethodName) => ObjectPropertyChangeMethodName = objectPropertyChangeMethodName;
    }
}