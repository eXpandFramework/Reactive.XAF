using System;

namespace Xpand.Extensions.XAF.Attributes.Custom{
    public class PercentageFormatAttribute(int decimals = 2) : Attribute, ICustomAttribute {
        public string Name => "MaskSettings";

        public string Value 
            => decimals switch {
                2 => "BAAAAA9NYXNrTWFuYWdlclR5cGUAggFEZXZFeHByZXNzLkRhdGEuTWFzay5OdW1lcmljTWFza01hbmFnZXIsIERldkV4cHJlc3MuRGF0YS52MjQuMSwgVmVyc2lvbj0yNC4xLjYuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iODhkMTc1NGQ3MDBlNDlhBG1hc2sHAgFwFmhpZGVJbnNpZ25pZmljYW50WmVyb3MKAgEYYXV0b0hpZGVEZWNpbWFsU2VwYXJhdG9yCgIBUseMaskAsDisplayFormat",
                0 => "BQAAAA9NYXNrTWFuYWdlclR5cGUAggFEZXZFeHByZXNzLkRhdGEuTWFzay5OdW1lcmljTWFza01hbmFnZXIsIERldkV4cHJlc3MuRGF0YS52MjQuMiwgVmVyc2lvbj0yNC4yLjMuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iODhkMTc1NGQ3MDBlNDlhBG1hc2sHAgJwMBZoaWRlSW5zaWduaWZpY2FudFplcm9zCgIBGGF1dG9IaWRlRGVjaW1hbFNlcGFyYXRvcgoCAQl2YWx1ZVR5cGUJUseMaskAsDisplayFormat",
                _ => throw new NotImplementedException(decimals.ToString())
            };
    }
}