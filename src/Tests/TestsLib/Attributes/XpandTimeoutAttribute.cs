using System;
using System.Diagnostics;
using NUnit.Framework;

namespace Xpand.TestsLib.Attributes{
    public class XpandTimeoutAttribute:TimeoutAttribute{
        public XpandTimeoutAttribute() : base(30000){
        }
    }
}