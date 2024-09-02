using System;
using DevExpress.Persistent.Validation;

namespace Xpand.Extensions.XAF.Attributes.Validation {
    [AttributeUsage(AttributeTargets.Property)]
    public class TimeSpanNotZeroAttribute() : RuleValueComparisonAttribute("TimeSpanNotEmpty", DefaultContexts.Save,
        ValueComparisonType.NotEquals, "#00:00:00#", ParametersMode.Expression);
}