using System;
using Xpand.Extensions.XAF.Attributes;

namespace Xpand.XAF.Modules.CloneModelView{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class CloneModelViewAttribute : Attribute, ICloneModelViewAttribute {
        public CloneModelViewAttribute(CloneViewType viewType, string viewId, bool isDefault = false) {
            ViewType = viewType;
            ViewId = viewId;
            IsDefault = isDefault;
        }

        public bool IsDefault { get; }
        public string ViewId { get; }
        public CloneViewType ViewType { get; }
        public string DetailView { get; set; }
    }
}