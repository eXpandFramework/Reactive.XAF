using System;

namespace Xpand.Extensions.XAF.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class ReadOnlyCollectionAttribute:Attribute,IReadOnlyAttribute {
        public bool AllowEdit { get; }
        public bool AllowDelete { get; }
        public bool AllowNew { get; }
        public bool DisableListViewProcess { get; }
        public bool AllowLinkUnLink{ get; }

        public ReadOnlyCollectionAttribute(bool allowEdit=false,bool allowDelete=false,bool allowNew=false,bool disableListViewProcess=true,bool  allowLinkUnLink=false) {
            DisableListViewProcess = disableListViewProcess;
            AllowLinkUnLink = allowLinkUnLink;
            AllowEdit = allowEdit;
            AllowDelete = allowDelete;
            AllowNew = allowNew;
        }
    }
}