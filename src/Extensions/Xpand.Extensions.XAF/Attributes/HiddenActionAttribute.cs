﻿using System;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Property,AllowMultiple = true)]
    public class HiddenActionAttribute:Attribute {
        public string[] Actions{ get; }

        public HiddenActionAttribute(params string[] actions) => Actions = actions;
    }
}