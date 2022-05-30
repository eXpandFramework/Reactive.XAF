using System;

namespace Xpand.Extensions.XAF.Attributes{
    public interface IReloadWhenChange {
        Action<string> WhenPropertyChanged { get; }
    }
    public class ReloadWhenChangeAttribute:Attribute {
        public ReloadWhenChangeAttribute(){
        }

        public string ObjectPropertyChangeMethodName{ get; }

        public ReloadWhenChangeAttribute(string objectPropertyChangeMethodName) => ObjectPropertyChangeMethodName = objectPropertyChangeMethodName;
    }
}