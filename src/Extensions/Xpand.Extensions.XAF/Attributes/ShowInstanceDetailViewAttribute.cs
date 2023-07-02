using System;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Class)]
    public class ShowInstanceDetailViewAttribute:Attribute {
        public ShowInstanceDetailViewAttribute(string property) => Property = property;

        public ShowInstanceDetailViewAttribute(){
        }

        public string Property { get; }
    }
}