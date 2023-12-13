using System;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple = true)]
    public class ColumnSummaryAttribute(SummaryType summaryType) : Attribute {
        public SummaryType SummaryType{ get; } = summaryType;
    }
}