using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.Xpo {
    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyConcatAttribute:Attribute,IXpoAttribute {
        public string Value { get; }

        public PropertyConcatAttribute(string[] names,string seperator=".") => Value = $"Concat({names.Join($",'{seperator}',")})";
        
        public static IEnumerable<IMemberInfo> Configure() 
            => XpoAttributesExtensions.Configure<PropertyConcatAttribute>();

    }

    internal class XpoAttributesExtensions {
        public static IEnumerable<IMemberInfo> Configure<T>() where T : Attribute,IXpoAttribute => XafTypesInfo.Instance.PersistentTypes.SelectMany(info => info.Members)
                .Select(info => {
                    var attribute = info.FindAttribute<T>();
                    if (attribute != null) {
                        info.AddAttribute(new PersistentAliasAttribute(attribute.Value));
                    }
                    return info;
                });

    }

    public interface IXpoAttribute {
        string Value { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SingleObjectAttribute:Attribute, IXpoAttribute {
        public string Value { get; }

        public SingleObjectAttribute(string collectionName,string collectionPropertyName,Aggregate aggregate=Aggregate.Max) {
            Value = $"{collectionName}[{collectionPropertyName}=^.{collectionName}.{aggregate}({collectionPropertyName})].Single()";
        }

        public static IEnumerable<IMemberInfo> Configure() 
            => XpoAttributesExtensions.Configure<SingleObjectAttribute>();
    }
}