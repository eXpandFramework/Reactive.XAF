using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Xpand.Source.Extensions.System.Refelction{
    [ExcludeFromCodeCoverage]
    internal sealed class DynamicPropertyInfo : PropertyInfo{
        readonly List<object> _attributesCore = new List<object>();

        public DynamicPropertyInfo(string name, Type propertyType, Type declaringType, bool canRead, bool canWrite){
            Name = name;
            PropertyType = propertyType;
            DeclaringType = declaringType;
            CanRead = canRead;
            CanWrite = canWrite;
        }

        public override string Name{ get; }

        public override Type PropertyType{ get; }

        public override Type DeclaringType { get; }

        public override bool CanRead { get; }

        public override bool CanWrite { get; }

        public override PropertyAttributes Attributes => throw new NotImplementedException();

        public override Type ReflectedType => throw new NotImplementedException();

        public override string ToString(){
            return $"{Name}-{DeclaringType}";
        }

        public override MethodInfo[] GetAccessors(bool nonPublic){
            throw new NotImplementedException();
        }

        public override MethodInfo GetGetMethod(bool nonPublic){
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetIndexParameters(){
            throw new NotImplementedException();
        }

        public override MethodInfo GetSetMethod(bool nonPublic){
            throw new NotImplementedException();
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index,
            CultureInfo culture){
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index,
            CultureInfo culture){
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit){
            return _attributesCore.Where(attributeType.IsInstanceOfType).ToArray();
        }

        public override object[] GetCustomAttributes(bool inherit){
            return _attributesCore.ToArray();
        }

        public override bool IsDefined(Type attributeType, bool inherit){
            throw new NotImplementedException();
        }

        public void RemoveAttribute(Attribute attribute){
            _attributesCore.Remove(attribute);
        }

        public void AddAttribute(Attribute attribute){
            _attributesCore.Add(attribute);
        }

    }
}
