using System;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple = true)]
    public class ColumnSummaryAttribute:Attribute {
        public SummaryType SummaryType{ get; }

        public ColumnSummaryAttribute( SummaryType summaryType) {
            SummaryType = summaryType;
        }
    }
}