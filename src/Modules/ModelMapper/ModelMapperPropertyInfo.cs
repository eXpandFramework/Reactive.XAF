using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;

namespace Xpand.XAF.Modules.ModelMapper{
    
    public  class ModelMapperType{
        public Type Type{ get; }
        public Type TypeToMap{ get; }

        public ModelMapperType(Type type, Type typeToMap){
            Type = type;
            TypeToMap = typeToMap;
            BaseTypeFullNames=new List<string>();
        }

        public List<string> BaseTypeFullNames{ get; }
    }

    public sealed class ModelMapperPropertyInfo : PropertyInfo{
        readonly List<ModelMapperCustomAttributeData> _customAttributeDatas = new List<ModelMapperCustomAttributeData>();


        private ModelMapperPropertyInfo(string name, Type propertyType, Type declaringType, bool canRead, bool canWrite){
            Name = name;
            PropertyType = propertyType;
            DeclaringType = declaringType;
            CanRead = canRead;
            CanWrite = canWrite;
        }

        public ModelMapperPropertyInfo(PropertyInfo propertyInfo) : this(propertyInfo.Name,
            propertyInfo.PropertyType, propertyInfo.DeclaringType, CustomAttributeDatas(propertyInfo)){
        }

        private static IEnumerable<ModelMapperCustomAttributeData> CustomAttributeDatas(PropertyInfo propertyInfo){
            return propertyInfo is ModelMapperPropertyInfo mapperPropertyInfo
                ? mapperPropertyInfo._customAttributeDatas
                : propertyInfo.GetCustomAttributesData().ToModelMapperConfigurationData();
        }

        public ModelMapperPropertyInfo(string name, Type propertyType,Type declaringType,IEnumerable<ModelMapperCustomAttributeData> customAttributeDatas = null) : this(name,
            propertyType, declaringType, true, true){
            customAttributeDatas = customAttributeDatas ?? Enumerable.Empty<ModelMapperCustomAttributeData>();
            _customAttributeDatas.AddRange(customAttributeDatas);
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

        public new IList<ModelMapperCustomAttributeData> GetCustomAttributesData(){
            return _customAttributeDatas;
        }

        public override ParameterInfo[] GetIndexParameters(){
            throw new NotImplementedException();
        }

        public override MethodInfo GetSetMethod(bool nonPublic){
            throw new NotImplementedException();
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index,CultureInfo culture){
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index,CultureInfo culture){
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit){
            throw new NotImplementedException($"Use the {nameof(GetCustomAttributesData)} method");
        }

        public override object[] GetCustomAttributes(bool inherit){
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit){
            throw new NotImplementedException();
        }


        public void RemoveAttributeData(ModelMapperCustomAttributeData customAttributeData){
            _customAttributeDatas.Remove(customAttributeData);
        }

        public void AddAttributeData(Type attributeType,params CustomAttributeTypedArgument[] arguments) {
            _customAttributeDatas.Add(new ModelMapperCustomAttributeData(attributeType,new List<CustomAttributeTypedArgument>(arguments)));
        }

        public void RemoveAttribute(Type type){
            if (Name == "ColVIndex"){
                Debug.WriteLine("");
            }
            var attributeData = _customAttributeDatas.First(_ => _.AttributeType==type);
            _customAttributeDatas.Remove(attributeData);
        }
    }

    public class ModelMapperCustomAttributeData:CustomAttributeData{
        public ModelMapperCustomAttributeData(Type attributeType, IList<CustomAttributeTypedArgument> constructorArguments){
            AttributeType = attributeType;
            ConstructorArguments = constructorArguments;
        }

        public override IList<CustomAttributeTypedArgument> ConstructorArguments{ get; }
        public new Type AttributeType{ get; }
        public override string ToString(){
            return $"{AttributeType.FullName}-({string.Join(",",ConstructorArguments.Select(_ => _.Value))})";
        }
    }
}
