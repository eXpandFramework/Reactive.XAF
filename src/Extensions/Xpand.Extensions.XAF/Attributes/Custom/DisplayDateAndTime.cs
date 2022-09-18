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
        [Description("hh:mm:ss")]
        hh_mm_ss,
        [Description("mm:ss")]
        mm_ss,
        [Description("mm:ss:fff")]
        mm_ss_fff
    }
    public class DisplayDateAndTime : Attribute, ICustomAttribute {
        private readonly string _dateString;
        private readonly string _timeString;
        public DisplayDateAndTime() { }

        public DisplayDateAndTime(DisplayDateType displayDateType = DisplayDateType.ddMMyy,DisplayTimeType displayTimeType=DisplayTimeType.hh_mm_ss) {
            _dateString = displayDateType.AsString(EnumFormat.Description);
            _timeString = displayTimeType.AsString(EnumFormat.Description);
        }
        string ICustomAttribute.Name => "DisplayFormat;EditMask;EditMaskType";

        string ICustomAttribute.Value => $"{{0: {_timeString}}};{_dateString} {_timeString};DateTime";
    }
}