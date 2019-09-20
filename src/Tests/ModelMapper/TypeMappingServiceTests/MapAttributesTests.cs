using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.Persistent.Base;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;


namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    [NonParallelizable]
    public class MapAttributesTests:ModelMapperBaseTest{
        [Test]
        public async Task Map_Private_DescriptionAttributes(){
            InitializeMapperService(nameof(Map_Private_DescriptionAttributes));
            
            var typeToMap = typeof(PrivateDescriptionAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var propertyInfo = modelType.Properties().First(info => info.Name == nameof(PrivateDescriptionAttributesClass.PrivateAttribute));
            var descriptionAttribute = propertyInfo.Attribute<DescriptionAttribute>();
            descriptionAttribute.ShouldNotBeNull();
            descriptionAttribute.Description.ShouldBe(PrivateDescriptionAttributesClass.Description);
        }

        [Test]
        public async Task Map_Attributes(){
            
            InitializeMapperService(nameof(Map_Attributes));
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
        public async Task Escape_strings(){

            InitializeMapperService(nameof(Escape_strings));

            var modelType = await typeof(EscapeAttributeString).MapToModel().ModelInterfaces();

            modelType.Properties().First(info => info.Name == nameof(EscapeAttributeString.Property))
                .Attribute<DescriptionAttribute>().Description.ShouldBe(EscapeAttributeString.Description);
        }


        [Test]
        public async Task Customize_Attributes_Mapping(){
            InitializeMapperService(nameof(Customize_Attributes_Mapping));
            TypeMappingService.PropertyMappingRules.Add(("Custom", tuple => {
                tuple.propertyInfos.First().AddAttributeData(typeof(DescriptionAttribute));
            }));
            
            var typeToMap = typeof(ReplaceAttributesClass);
            
            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            modelType.Properties().First().GetCustomAttributes(typeof(DescriptionAttribute),false).Cast<DescriptionAttribute>().Any().ShouldBeTrue();
        }

        [Test]
        public async Task Attribute_Mapping_Can_Be_Disabled(){
            InitializeMapperService(nameof(Attribute_Mapping_Can_Be_Disabled));
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
