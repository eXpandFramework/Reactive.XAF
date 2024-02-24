using System;
using DevExpress.Data;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnSortingAttribute(ColumnSortOrder sortOrder=ColumnSortOrder.Ascending, int sortIndex=0):Attribute {
        public ColumnSortOrder SortOrder{ get; } = sortOrder;
        public int SortIndex { get; } = sortIndex;
    }
}