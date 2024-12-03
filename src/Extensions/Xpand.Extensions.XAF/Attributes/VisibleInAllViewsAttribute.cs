using System;

namespace Xpand.Extensions.XAF.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class VisibleInAllViewsAttribute:Attribute {
        public VisibleInAllViewsAttribute(bool createModelMember) => CreateModelMember = createModelMember;

        public VisibleInAllViewsAttribute(){
        }

        public bool CreateModelMember { get; }
    }
}