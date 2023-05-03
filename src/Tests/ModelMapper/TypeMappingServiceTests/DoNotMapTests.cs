using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;

using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    [NonParallelizable]
    public class DoNotMapTests:ModelMapperCommonTest{
        [Test]
        
        public async Task Do_Not_Map_If_recursion_detected(){
            
            InitializeMapperService();
            var typeToMap = typeof(RootType);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();
            
            var modelTypeProperties = modelType.Properties();

            modelTypeProperties.FirstOrDefault(info => info.Name==nameof(RootType.Self)).ShouldBeNull();
            modelTypeProperties.FirstOrDefault(info => info.Name==nameof(RootType.Value)).ShouldNotBeNull();
            var mapperInfo = modelTypeProperties.FirstOrDefault(info => info.Name==nameof(RootType.RootTestModelMapper));
            mapperInfo.ShouldNotBeNull();
            mapperInfo.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(TestModelMapper.Name)).ShouldNotBeNull();
            var nestedInfo = modelTypeProperties.FirstOrDefault(info => info.Name==nameof(RootType.NestedType));
            nestedInfo.ShouldNotBeNull();

            nestedInfo.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(NestedType.RootType)).ShouldBeNull();
            mapperInfo=nestedInfo.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(NestedType.NestedTestModelMapper));
            mapperInfo.ShouldNotBeNull();
            mapperInfo.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(TestModelMapper.Name)).ShouldNotBeNull();


            var nested2Info = nestedInfo.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(NestedType.NestedType2));
            nested2Info.ShouldNotBeNull();
            nested2Info.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(NestedType2.Nested2TestModelMapper)).ShouldNotBeNull();
            nested2Info.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(NestedType2.NestedType)).ShouldBeNull();

        }

        [Test()][Ignore(NotImplemented)]
        
        public  Task Do_not_map_Objects_with_no_MapAble_properties(){
//            InitializeMapperService(nameof(Do_not_map_Objects_with_no_MapAble_properties));
            throw new NotImplementedException();
        }

        [Test]
        
        public async Task Do_Not_Map_TypeConverterAttributes_with_DevExpress_DesignTime_Types(){
            InitializeMapperService();
            var typeToMap = typeof(DXDesignTimeAttributeClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.First().Attribute<TypeConverterAttribute>().ShouldBeNull();
        }


        [Test]
        
        public async Task Do_Not_Map_Reserved_properties(){
            InitializeMapperService();
            var typeToMap = typeof(ResevredProperties);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.Length.ShouldBe(0);

        }

        [Test]
        
        public async Task Do_Not_Map_Non_Browsable_properties(){
            InitializeMapperService();
            var typeToMap = typeof(NonBrowsableProperties);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.FirstOrDefault(info => info.Name==nameof(NonBrowsableProperties.NonBroswsableTest)).ShouldBeNull();
            modelTypeProperties.FirstOrDefault(info => info.Name==nameof(NonBrowsableProperties.Test)).ShouldNotBeNull();

        }

        [Test]
        
        public async Task Do_Not_Map_Obsolete_properties(){
            InitializeMapperService();
            var typeToMap = typeof(ObsoleteProperties);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.FirstOrDefault(info => info.Name=="ObsoleteTest").ShouldBeNull();
            modelTypeProperties.FirstOrDefault(info => info.Name==nameof(ObsoleteProperties.Test)).ShouldNotBeNull();

        }

        [Test]
        
        public async Task Do_Not_Map_Already_Mapped_Types(){
            var typeToMap1 = typeof(TestModelMapper);
            var typeToMap2 = typeof(TestModelMapper);
            InitializeMapperService();

            await typeToMap1.MapToModel().ModelInterfaces();
            await typeToMap2.MapToModel().ModelInterfaces();
            
            TypeMappingService.MappedTypes.ToEnumerable().Count().ShouldBe(1);
            
        }

        [Test]
        
        public async Task Do_not_Map_DefaultValueAttribute(){

            InitializeMapperService();
            
            var typeToMap = typeof(DefaultValueAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            modelType.Properties().First().GetCustomAttributes(typeof(DefaultValueAttribute),false).Cast<DefaultValueAttribute>().Any().ShouldBeFalse();
            
        }

        [Test]
        
        public async Task Do_not_Map_Attributes_With_Flag_Parameters(){

            InitializeMapperService();
            
            var typeToMap = typeof(FlagAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            modelType.Properties().First(info => info.Name==nameof(FlagAttributesClass.FlagPropertyValue)).Attribute<FlagParameterAttribute>().ShouldBeNull();
            modelType.Properties().First(info => info.Name==nameof(FlagAttributesClass.FlagProperty)).Attribute<FlagParameterAttribute>().ShouldNotBeNull();
            
        }

        [Test]
        
        public async Task Do_Not_Map_Attributes_With_Non_Public_Types_As_Parameters(){
            var typeToMap1 = typeof(NonPublicAttributeClass);
            InitializeMapperService();

            var type = await typeToMap1.MapToModel().ModelInterfaces();

            type.Property(nameof(NonPublicAttributeClass.Test)).Attributes().Any().ShouldBeFalse();
            
        }

        [Test]
        
        public async Task Do_Not_Map_Properties_Marked_With_DesignerSerialization_Hidden(){
            var typeToMap1 = typeof(DesignerSerializationHiddenClass);
            InitializeMapperService();

            var type = await typeToMap1.MapToModel().ModelInterfaces();

            type.Property(nameof(DesignerSerializationHiddenClass.Test)).ShouldBeNull();
            
        }

        [Test]
        
        public async Task Do_Not_Map_Generic_Type_Properties(){
            var typeToMap1 = typeof(GenericTypeProperties);
            InitializeMapperService();

            var type = await typeToMap1.MapToModel().ModelInterfaces();

            type.Property(nameof(GenericTypeProperties.Test)).ShouldBeNull();
            
        }

        [Test]
        
        public async Task Do_Not_Map_Tuple_Type_Properties(){
            var typeToMap1 = typeof(TupleTypeProperties);
            InitializeMapperService();

            var type = await typeToMap1.MapToModel().ModelInterfaces();

            type.Property(nameof(TupleTypeProperties.Test)).ShouldBeNull();
            
        }

        


    }
}
