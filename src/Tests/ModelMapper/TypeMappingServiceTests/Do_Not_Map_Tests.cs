using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Fasterflect;
using Mono.Cecil;
using Shouldly;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xunit;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    
    public partial class ObjectMappingServiceTests{
        [Fact]
        public async Task Do_Not_Map_Already_Mapped_Properties(){
            
            InitializeMapperService(nameof(Do_Not_Map_Already_Mapped_Properties));
            var typeToMap = typeof(RootType);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();
            
            var modelTypeProperties = modelType.Properties();

            modelTypeProperties.FirstOrDefault(info => info.Name==nameof(RootType.Self)).ShouldBeNull();
            modelTypeProperties.FirstOrDefault(info => info.Name==nameof(RootType.Value)).ShouldNotBeNull();
            var mapperInfo = modelTypeProperties.FirstOrDefault(info => info.Name==nameof(RootType.RootTestModelMapper));
            mapperInfo.ShouldNotBeNull();
            mapperInfo.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(TestModelMapper.Age)).ShouldNotBeNull();
            var nestedInfo = modelTypeProperties.FirstOrDefault(info => info.Name==nameof(RootType.NestedType));
            nestedInfo.ShouldNotBeNull();

            nestedInfo.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(NestedType.RootType)).ShouldBeNull();
            mapperInfo=nestedInfo.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(NestedType.NestedTestModelMapper));
            mapperInfo.ShouldNotBeNull();
            mapperInfo.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(TestModelMapper.Age)).ShouldNotBeNull();


            var nested2Info = nestedInfo.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(NestedType.NestedType2));
            nested2Info.ShouldNotBeNull();
            nested2Info.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(NestedType2.Nested2TestModelMapper)).ShouldNotBeNull();
            nested2Info.PropertyType.Properties().FirstOrDefault(info => info.Name==nameof(NestedType2.NestedType)).ShouldBeNull();

        }

        [Fact(Skip = NotImplemented)]
        public async Task Do_not_map_Objects_with_no_mapable_properties(){
            InitializeMapperService(nameof(Do_not_map_Objects_with_no_mapable_properties));
//            throw new NotImplementedException();
        }

        [Fact()]
        public async Task Do_Not_Map_TypeConverterAttributes_with_DevExpress_DesignTime_Types(){
            InitializeMapperService(nameof(Do_Not_Map_TypeConverterAttributes_with_DevExpress_DesignTime_Types));
            var typeToMap = typeof(DXDesignTimeAttributeClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.First().Attribute<TypeConverterAttribute>().ShouldBeNull();
        }


        [Fact]
        public async Task Do_Not_Map_Reserved_properties(){
            InitializeMapperService(nameof(Do_Not_Map_Reserved_properties));
            var typeToMap = typeof(ResevredProperties);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.Length.ShouldBe(0);

        }

        [Fact]
        public async Task Do_Not_Map_Non_Browsable_properties(){
            InitializeMapperService(nameof(Do_Not_Map_Non_Browsable_properties));
            var typeToMap = typeof(NonBrowsableProperties);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.FirstOrDefault(info => info.Name==nameof(NonBrowsableProperties.NonBroswsableTest)).ShouldBeNull();
            modelTypeProperties.FirstOrDefault(info => info.Name==nameof(NonBrowsableProperties.Test)).ShouldNotBeNull();

        }

        [Fact]
        public async Task Do_Not_Map_Already_Mapped_Types(){
            var typeToMap1 = typeof(TestModelMapper);
            var typeToMap2 = typeof(TestModelMapper);
            InitializeMapperService(nameof(Do_Not_Map_Already_Mapped_Types));

            await typeToMap1.MapToModel().ModelInterfaces();
            await typeToMap2.MapToModel().ModelInterfaces();
            
            TypeMappingService.MappedTypes.ToEnumerable().Count().ShouldBe(1);
            
        }

        [Fact]
        public async Task Do_not_Map_DefaultValueAttribute(){

            InitializeMapperService(nameof(Do_not_Map_DefaultValueAttribute));
            
            var typeToMap = typeof(DefaultValueAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            modelType.Properties().First().GetCustomAttributes(typeof(DefaultValueAttribute),false).Cast<DefaultValueAttribute>().Any().ShouldBeFalse();
            
        }

        [Fact]
        public async Task Do_not_Map_Attributes_With_Flag_Parameters(){

            InitializeMapperService(nameof(Do_not_Map_Attributes_With_Flag_Parameters));
            
            var typeToMap = typeof(FlagAttributesClass);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            modelType.Properties().First(info => info.Name==nameof(FlagAttributesClass.FlagPropertyValue)).Attribute<FlagParameterAttribute>().ShouldBeNull();
            modelType.Properties().First(info => info.Name==nameof(FlagAttributesClass.FlagProperty)).Attribute<FlagParameterAttribute>().ShouldNotBeNull();
            
        }

        [Fact]
        public async Task Do_Not_Map_Attributes_With_Non_Public_Types_As_Parameters(){
            var typeToMap1 = typeof(NonPublicAttributeClass);
            InitializeMapperService(nameof(Do_Not_Map_Attributes_With_Non_Public_Types_As_Parameters));

            var type = await typeToMap1.MapToModel().ModelInterfaces();

            type.Property(nameof(NonPublicAttributeClass.Test)).Attributes().Any().ShouldBeFalse();
            
        }

        
        public async Task Do_Not_Map_PredifinedMap_Columns(){
//            InitializeMapperService($"{nameof(Do_Not_Map_PredifinedMap_Columns)}",Platform.Win);
//            ConfigureLayoutViewPredifinedMapService();
//            var values = Enums.GetValues<PredifinedMap>().Where(map =>
//                map.GetAttributes().OfType<MapPlatformAttribute>().Any(_ => _.Platform == platform.ToString())).ToArray();
//
//            var modelInterfaces = values.MapToModel().ModelInterfaces().Replay();
//            modelInterfaces.Connect();
//
//            var types = modelInterfaces.ToEnumerable().ToArray();
//            types.Length.ShouldBe(values.Length);
//            foreach (var configuration in values){
//                types.FirstOrDefault(_ => _.Name==$"IModel{configuration.ToString()}").ShouldNotBeNull();
//            }
        }

        [Fact]
        public async Task Do_Not_Map_If_Type_Assembly_Version_Not_Changed(){
            InitializeMapperService(nameof(Do_Not_Map_If_Type_Assembly_Version_Not_Changed));
            var mappedType = typeof(TestModelMapper);

            var mapToModel = await mappedType.MapToModel().ModelInterfaces();

            var modelMapperAttribute = mapToModel.Assembly.GetCustomAttributes(typeof(ModelMapperServiceAttribute),false)
                .OfType<ModelMapperServiceAttribute>().FirstOrDefault(attribute => attribute.MappedType==mappedType.FullName&&attribute.MappedAssemmbly==mappedType.Assembly.GetName().Name);
            modelMapperAttribute.ShouldNotBeNull();

            var version = modelMapperAttribute.Version;

            mappedType.MapToModel();
            mapToModel = await TypeMappingService.MappedTypes;

            modelMapperAttribute = mapToModel.Assembly.GetCustomAttributes(typeof(ModelMapperServiceAttribute),false)
                .OfType<ModelMapperServiceAttribute>().First(attribute => attribute.MappedType==mappedType.FullName&&attribute.MappedAssemmbly==mappedType.Assembly.GetName().Name);
            modelMapperAttribute.Version.ShouldBe(version);
        }

    }
}
