using System;

namespace Xpand.XAF.Modules.ViewItemValue{
    [AttributeUsage(AttributeTargets.Property)]
    public class ViewItemValueAttribute:Attribute {
        public SaveViewItemValueStrategy SaveViewItemValueStrategy { get; set; }=SaveViewItemValueStrategy.OnCommit; 
    }
}