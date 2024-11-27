using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using EnumsNET;

namespace Xpand.Extensions.XAF.Attributes.Custom {
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum DisplayDateType {
        [Description("")]
        None,
        [Description("dd/MM/yy")]
        ddMMyy
    }
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum DisplayTimeType {
        [Description("HH:mm:ss")]
        hh_mm_ss,
        [Description("mm:ss")]
        mm_ss,
        [Description("HH:mm")]
        hh_mm,
        [Description("mm:ss:fff")]
        mm_ss_fff
    }
    public class DisplayDateAndTime(
        DisplayDateType displayDateType = DisplayDateType.ddMMyy,
        DisplayTimeType displayTimeType = DisplayTimeType.hh_mm_ss)
        : Attribute, ICustomAttribute {
        private readonly string _dateString = displayDateType.AsString(EnumFormat.Description);
        private readonly string _timeString = displayTimeType.AsString(EnumFormat.Description);
        public DisplayDateAndTime():this(DisplayDateType.ddMMyy) { }

        string ICustomAttribute.Name => "DisplayFormat;EditMask;EditMaskType";

        string ICustomAttribute.Value => $"{{0: {_dateString} {_timeString}}};{_dateString} {_timeString};DateTime";
    }
    
}