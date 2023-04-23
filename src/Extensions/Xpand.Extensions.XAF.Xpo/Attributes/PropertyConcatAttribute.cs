using System;
using System.Collections;
using System.Collections.Generic;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.Xpo.Attributes{
    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyConcatAttribute:Attribute,IXpoAttributeValue {
        public string Value { get; }

        public PropertyConcatAttribute(string[] names,string separator=".") => Value = $"Concat({LinqExtensions.LinqExtensions.Join((IEnumerable)names, $",'{separator}',")})";
        
        public static IMemberInfo[] Configure() 
            => XpoExtensions.Configure<PropertyConcatAttribute>();

    }
}