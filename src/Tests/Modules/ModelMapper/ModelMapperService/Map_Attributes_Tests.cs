using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.Persistent.Base;
using Fasterflect;
using Shouldly;
using Xpand.XAF.Modules.ModelMapper;
using Xunit;

namespace Tests.Modules.ModelMapper.ModelMapperService{
    
    public partial class ModelMapperServiceTests:ModelMapperBaseTest{
        [Fact]
        public async Task Map_Attributes(){
            InitializeMapperService(nameof(Map_Attributes));
            var typeToMap = typeof(CopyAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var propertyInfos = modelType.Properties();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeNoParam))
                .GetCustomAttributes(typeof(DescriptionAttribute),false).FirstOrDefault().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributePrivate))
                .GetCustomAttributes(typeof(Attribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeValueTypeParam))
                .GetCustomAttributes(typeof(IndexAttribute),false).FirstOrDefault().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeDefaultVvalueAttribue))
                .GetCustomAttributes(typeof(DefaultValueAttribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeStringParam))
                .GetCustomAttributes(typeof(DescriptionAttribute),false).FirstOrDefault().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeTwoParam))
                .GetCustomAttributes(typeof(MyClassAttribute),false).FirstOrDefault().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeTypeParam))
                .GetCustomAttributes(typeof(TypeConverterAttribute),false).FirstOrDefault().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeEnumParam))
                .GetCustomAttributes(typeof(MyClassAttribute),false).FirstOrDefault().ShouldNotBeNull();
        }

        [Fact]
        public async Task Map_Private_DescriptionAttributes(){
            InitializeMapperService(nameof(Map_Private_DescriptionAttributes));
            
            var typeToMap = typeof(PrivateDescriptionAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            modelType.Properties().First().GetCustomAttributes(typeof(DescriptionAttribute),false).Cast<DescriptionAttribute>().Any().ShouldBeTrue();
            
        }

        [Fact]
        public async Task Attributes_Can_Be_Replaced(){
            InitializeMapperService(nameof(Attributes_Can_Be_Replaced));
            Xpand.XAF.Modules.ModelMapper.ModelMapperService.AttributesMap.Add(typeof(PrivateAttribute),(typeof(DescriptionAttribute),attribute => null));
            var typeToMap = typeof(ReplaceAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            modelType.Properties().First().GetCustomAttributes(typeof(DescriptionAttribute),false).Cast<DescriptionAttribute>().Any().ShouldBeTrue();
            
        }

        [Fact]
        public async Task Attribute_Mapping_Can_Be_Disabled(){
            InitializeMapperService(nameof(Attributes_Can_Be_Replaced));
            var typeToMap = typeof(CopyAttributesClass);

            var modelType = await typeToMap.MapToModel(new ModelMapperConfiguration(){DisableAttributeMapping=true}).ModelInterfaces();

            var propertyInfos = modelType.Properties();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeNoParam))
                .GetCustomAttributes(typeof(DescriptionAttribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributePrivate))
                .GetCustomAttributes(typeof(Attribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeValueTypeParam))
                .GetCustomAttributes(typeof(IndexAttribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeDefaultVvalueAttribue))
                .GetCustomAttributes(typeof(DefaultValueAttribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeStringParam))
                .GetCustomAttributes(typeof(DescriptionAttribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeTwoParam))
                .GetCustomAttributes(typeof(MyClassAttribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeTypeParam))
                .GetCustomAttributes(typeof(TypeConverterAttribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeEnumParam))
                .GetCustomAttributes(typeof(MyClassAttribute),false).FirstOrDefault().ShouldBeNull();
            
        }
    }
}
