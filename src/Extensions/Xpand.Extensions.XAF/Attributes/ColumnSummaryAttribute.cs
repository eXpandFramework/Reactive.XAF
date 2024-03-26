using System;
using DevExpress.Data;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple = true)]
    public class ColumnSummaryAttribute(SummaryType summaryType,SummaryMode summaryMode=SummaryMode.Mixed) : Attribute {
        public SummaryType SummaryType{ get; } = summaryType;
        public SummaryMode SummaryMode{ get; } = summaryMode;
    }
}