using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.Xpo.Attributes{
    [AttributeUsage(AttributeTargets.Property)]
    public class SingleObjectAttribute:Attribute, IXpoAttributeValue {
        public string Value { get; }

        public SingleObjectAttribute(string collectionName,string collectionPropertyName,Aggregate aggregate=Aggregate.Max) {
            Value = $"{collectionName}[{collectionPropertyName}=^.{collectionName}.{aggregate}({collectionPropertyName})].Single()";
        }

        public static IMemberInfo[] Configure() 
            => XpoExtensions.Configure<SingleObjectAttribute>();
    }
}