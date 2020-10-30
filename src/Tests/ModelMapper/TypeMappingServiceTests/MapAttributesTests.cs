using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.Persistent.Base;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;


namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    [NonParallelizable]
    public class MapAttributesTests:ModelMapperBaseTest{
        [Test]
        [XpandTest]
        public async Task Map_Private_DescriptionAttributes(){
            InitializeMapperService();
            
            var typeToMap = typeof(PrivateDescriptionAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var propertyInfo = modelType.Properties().First(info => info.Name == nameof(PrivateDescriptionAttributesClass.PrivateAttribute));
            var descriptionAttribute = propertyInfo.Attribute<DescriptionAttribute>();
            descriptionAttribute.ShouldNotBeNull();
            descriptionAttribute.Description.ShouldBe(PrivateDescriptionAttributesClass.Description);
        }

        [Test]
        [XpandTest]
        public async Task Map_Attributes(){
            
            InitializeMapperService();
            var typeToMap = typeof(CopyAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var propertyInfos = modelType.Properties();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.NestedTypeArgument)).Attribute<TypeConverterAttribute>().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeNoParam)).Attribute<DescriptionAttribute>().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributePrivate)).Attribute<Attribute>().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeValueTypeParam)).Attribute<IndexAttribute>().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeDefaultVvalueAttribue)).Attribute<DefaultValueAttribute>().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeStringParam)).Attribute<DescriptionAttribute>().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeTwoParam)).Attribute<MyClassAttribute>().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeTypeParam)).Attribute<TypeConverterAttribute>().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeEnumParam)).Attribute<MyClassAttribute>().ShouldNotBeNull();
        }


        [Test]
        [XpandTest]
        public async Task Escape_strings(){

            InitializeMapperService();

            var modelType = await typeof(EscapeAttributeString).MapToModel().ModelInterfaces();

            modelType.Properties().First(info => info.Name == nameof(EscapeAttributeString.Property))
                .Attribute<DescriptionAttribute>().Description.ShouldBe(EscapeAttributeString.Description);
        }


        [Test]
        [XpandTest]
        public async Task Customize_Attributes_Mapping(){
            InitializeMapperService();
            TypeMappingService.PropertyMappingRules.Add(("Custom", tuple => {
                tuple.propertyInfos.First().AddAttributeData(typeof(DescriptionAttribute));
            }));
            
            var typeToMap = typeof(ReplaceAttributesClass);
            
            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            modelType.Properties().First().GetCustomAttributes(typeof(DescriptionAttribute),false).Cast<DescriptionAttribute>().Any().ShouldBeTrue();
        }

        [Test]
        [XpandTest]
        public async Task Remove_LocalizableAttribute_from_Non_String_Properties(){
            InitializeMapperService();
            var typeToMap = typeof(LocalizableAttributeClass);
            
            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var propertyInfos = modelType.Properties();
            propertyInfos.First(info => info.Name==nameof(LocalizableAttributeClass.BackColor)).GetCustomAttributes(typeof(LocalizableAttribute),false).Any().ShouldBeFalse();
            propertyInfos.First(info => info.Name==nameof(LocalizableAttributeClass.Color)).GetCustomAttributes(typeof(LocalizableAttribute),false).Any().ShouldBeTrue();
        }

        [Test]
        [XpandTest]
        public async Task Attribute_Mapping_Can_Be_Disabled(){
            InitializeMapperService();
            TypeMappingService.PropertyMappingRules.Add(("Disable", tuple => {
                foreach (var modelMapperPropertyInfo in tuple.propertyInfos){
                    foreach (var modelMapperCustomAttributeData in modelMapperPropertyInfo.GetCustomAttributesData().ToArray()){
                        modelMapperPropertyInfo.RemoveAttributeData(modelMapperCustomAttributeData);
                    }
                }
            }));
            var typeToMap = typeof(CopyAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var propertyInfos = modelType.Properties();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeNoParam)).Attribute<DescriptionAttribute>().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributePrivate)).Attribute<Attribute>().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeValueTypeParam)).Attribute<IndexAttribute>().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeDefaultVvalueAttribue)).Attribute<DefaultValueAttribute>().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeStringParam)).Attribute<DescriptionAttribute>().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeTwoParam)).Attribute<MyClassAttribute>().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeTypeParam)).Attribute<TypeConverterAttribute>().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeEnumParam)).Attribute<MyClassAttribute>().ShouldBeNull();
        }
    }
}
