using System;

namespace Xpand.Extensions.XAF.Attributes.Custom{
    public class PercentageFormatAttribute:Attribute,ICustomAttribute {
        public string Name => "MaskSettings";

        public string Value => "BAAAAA9NYXNrTWFuYWdlclR5cGUAggFEZXZFeHByZXNzLkRhdGEuTWFzay5OdW1lcmljTWFza01hbmFnZXIsIERldkV4cHJlc3MuRGF0YS52MjQuMSwgVmVyc2lvbj0yNC4xLjYuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iODhkMTc1NGQ3MDBlNDlhBG1hc2sHAgFwFmhpZGVJbnNpZ25pZmljYW50WmVyb3MKAgEYYXV0b0hpZGVEZWNpbWFsU2VwYXJhdG9yCgIBUseMaskAsDisplayFormat";
    }
}