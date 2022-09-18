using System;

namespace Xpand.Extensions.XAF.Attributes {
    public enum OperationLayer {
        Model, Appearance
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class InvisibleInAllViewsAttribute:Attribute {
        public OperationLayer Layer { get; }

        public InvisibleInAllViewsAttribute(OperationLayer layer = OperationLayer.Model) 
            => Layer = layer;
    }
}