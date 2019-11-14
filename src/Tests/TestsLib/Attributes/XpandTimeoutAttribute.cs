using System;
using System.Diagnostics;
using NUnit.Framework;

namespace Xpand.TestsLib.Attributes{
    public class XpandTimeoutAttribute:TimeoutAttribute{
        public XpandTimeoutAttribute() : base((int) (TimeSpan.FromSeconds(Debugger.IsAttached?120:5).TotalMilliseconds)){
        }
    }
}