using System;
using System.IO;
using System.Runtime.CompilerServices;
using DevExpress.Persistent.Validation;

namespace Xpand.Extensions.XAF.Attributes.Validation {
    [AttributeUsage(AttributeTargets.Property)]
    public class TimeSpanNotZeroAttribute(
        [CallerFilePath] string callerFilePath = null,
        [CallerMemberName] string caller = "")
        : RuleValueComparisonAttribute($"TimeSpanNotEmpty{caller}{Path.GetFileNameWithoutExtension(callerFilePath)}",
            DefaultContexts.Save,
            ValueComparisonType.NotEquals, "#00:00:00#", ParametersMode.Expression);
}