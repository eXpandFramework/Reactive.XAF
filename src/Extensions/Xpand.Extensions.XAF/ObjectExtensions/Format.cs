using DevExpress.Persistent.Base;

namespace Xpand.Extensions.XAF.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static string Format(this object value,object instance,EmptyEntriesMode emptyEntriesMode=EmptyEntriesMode.RemoveDelimiterWhenEntryIsEmpty) 
            => ObjectFormatter.Format($"{value}", instance,emptyEntriesMode);
    }
}