using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using EnumsNET;
using Fasterflect;
using Shouldly;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xunit;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests
{

    public partial class ObjectMappingServiceTests{
        [Fact]
        public async Task Map_RW_StringValueType_Public_Properties(){
            InitializeMapperService(nameof(Map_RW_StringValueType_Public_Properties));
            var typeToMap = typeof(StringValueTypeProperties);
            var propertiesToMap = typeToMap.Properties().Where(info => info.CanRead&&info.CanWrite).ToArray();

            var modelType = await typeToMap.MapToModel().ModelInterfaces();


            var modelTypeProperties = ModelTypeProperties(modelType);
            foreach (var propertyInfo in propertiesToMap){
                var modelProperty = modelTypeProperties.FirstOrDefault(info => info.Name==propertyInfo.Name);
                modelProperty.ShouldNotBeNull(propertyInfo.Name);
                var modelPropertyPropertyType = modelProperty.PropertyType;
                var propertyInfoPropertyType = propertyInfo.PropertyType;
                if (!propertyInfoPropertyType.IsGenericType){
                    if (propertyInfoPropertyType.IsValueType){
                        modelPropertyPropertyType.GetGenericTypeDefinition().ShouldBe(typeof(Nullable<>));
                        modelPropertyPropertyType.GetGenericArguments().First().ShouldBe(propertyInfoPropertyType);
                    }
                    else{
                        modelPropertyPropertyType.ShouldBe(propertyInfoPropertyType);
                    }
                }
                else{
                    modelPropertyPropertyType.ShouldBe(propertyInfoPropertyType);
                }

            }
            modelTypeProperties.Length.ShouldBe(propertiesToMap.Length);
        }

        [Fact]
        public async Task Map_All_ReferenceType_Public_Properties(){
            InitializeMapperService(nameof(Map_All_ReferenceType_Public_Properties));
            var typeToMap = typeof(ReferenceTypeProperties);
            var propertiesToMap = typeToMap.Properties().ToArray();

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            foreach (var propertyInfo in propertiesToMap){
                var modelProperty = modelTypeProperties.FirstOrDefault(info => info.Name==propertyInfo.Name);
                modelProperty.ShouldNotBeNull(propertyInfo.Name);
                modelProperty.PropertyType.Name.ShouldBe($"{propertyInfo.PropertyType.ModelMapName(typeToMap)}");
            }

            modelTypeProperties.Length.ShouldBe(propertiesToMap.Length);
        }

        [Fact]
        public async Task Map_Nested_type_properties(){
            InitializeMapperService(nameof(Map_Nested_type_properties));
            var typeToMap = typeof(NestedTypeProperties);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.Length.ShouldBe(1);

        }


        [Theory]
        [InlineData(PredifinedMap.GridColumn,typeof(GridColumn),Platform.Win)]
        [InlineData(PredifinedMap.GridView,typeof(GridView),Platform.Win)]
        [InlineData(PredifinedMap.ASPxGridView,typeof(GridView),Platform.Web)]
        [InlineData(PredifinedMap.GridViewColumn,typeof(GridView),Platform.Web)]
        internal async Task Map_PredifinedConfigurations(PredifinedMap configuration,Type assemblyToLoad,Platform platform){

            InitializeMapperService($"{nameof(Map_PredifinedConfigurations)}{configuration}",platform);
            
            var modelType = await configuration.MapToModel().ModelInterfaces();

            modelType.Name.ShouldBe($"IModel{configuration}");

            var propertyInfos = modelType.Properties();
            var descriptionAttribute = propertyInfos.Select(info => info.Attribute<DescriptionAttribute>())
                .FirstOrDefault(attribute => attribute != null && attribute.Description.Contains(" ") );
            descriptionAttribute.ShouldNotBeNull();

        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void Map_All_PredifinedConfigurations(Platform platform){

            InitializeMapperService($"{nameof(Map_All_PredifinedConfigurations)}",platform);
            var values = Enums.GetValues<PredifinedMap>().Where(map =>
                    map.GetAttributes().OfType<MapPlatformAttribute>().Any(_ => _.Platform == platform.ToString())).ToArray();

            var modelInterfaces = values.MapToModel().ModelInterfaces().Replay();
            modelInterfaces.Connect();

            var types = modelInterfaces.ToEnumerable().ToArray();
            types.Length.ShouldBe(values.Length);
            foreach (var configuration in values){
                types.FirstOrDefault(_ => _.Name==$"IModel{configuration.ToString()}").ShouldNotBeNull();
            }
        }
        [Fact]
        internal void Map_PredifinedConfigurations_Combination(){

            InitializeMapperService($"{nameof(Map_All_PredifinedConfigurations)}",Platform.Win);
            

            var modelInterfaces = new[]{PredifinedMap.GridView,PredifinedMap.GridColumn}.MapToModel().ModelInterfaces().Replay();
            modelInterfaces.Connect();

            var types = modelInterfaces.ToEnumerable().ToArray();
            types.Length.ShouldBe(2);
            types.First().Name.ShouldBe($"IModel{PredifinedMap.GridView}");
            types.Last().Name.ShouldBe($"IModel{PredifinedMap.GridColumn}");
        }

        [Fact]
        public async Task Map_Multiple_Objects_from_the_same_subscription_In_the_same_assembly(){
            var typeToMap1 = typeof(TestModelMapper);
            var typeToMap2 = typeof(StringValueTypeProperties);
            InitializeMapperService(nameof(Map_Multiple_Objects_from_the_same_subscription_In_the_same_assembly));

            var mappedTypes = new[]{typeToMap1, typeToMap2}.MapToModel().ModelInterfaces();

            var mappedType1 = await mappedTypes.Take(1);
            mappedType1.Name.ShouldBe($"IModel{typeToMap1.Name}");
            var mappedType2 = await mappedTypes.Take(2);
            mappedType2.Name.ShouldBe($"IModel{typeToMap2.Name}");
            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

        [Fact]
        public async Task Map_Multiple_Objects_with_common_types(){
            var typeToMap1 = typeof(TestModelMapperCommonType1);
            var typeToMap2 = typeof(TestModelMapperCommonType2);
            InitializeMapperService(nameof(Map_Multiple_Objects_with_common_types));

            var mappedTypes = new[]{typeToMap1, typeToMap2}.MapToModel().ModelInterfaces();

            var mappedType1 = await mappedTypes.Take(1);
            mappedType1.Name.ShouldBe($"IModel{typeToMap1.Name}");
            var appearenceCell = mappedType1.Properties().First(_ => _.Name==nameof(TestModelMapperCommonType1.AppearanceCell));
            appearenceCell.ShouldNotBeNull();
            appearenceCell.GetType().Properties("TextOptions").ShouldNotBeNull();
            var mappedType2 = await mappedTypes.Take(2);
            mappedType2.Name.ShouldBe($"IModel{typeToMap2.Name}");
            appearenceCell = mappedType1.Properties().First(_ => _.Name==nameof(TestModelMapperCommonType2.AppearanceCell));
            appearenceCell.ShouldNotBeNull();
            appearenceCell.GetType().Properties("TextOptions").ShouldNotBeNull();
            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

        [Fact]
        public async Task Map_Multiple_Objects_from_the_different_subscription_In_the_same_assembly(){
            var typeToMap1 = typeof(TestModelMapper);
            var typeToMap2 = typeof(StringValueTypeProperties);
            InitializeMapperService(nameof(Map_Multiple_Objects_from_the_different_subscription_In_the_same_assembly));

            await new[]{typeToMap1}.MapToModel();
            await new[]{typeToMap2}.MapToModel();

            TypeMappingService.Start();
            var mappedType1 = await TypeMappingService.MappedTypes.Take(1);
            mappedType1.Name.ShouldBe($"IModel{typeToMap1.Name}");
            var mappedType2 = await TypeMappingService.MappedTypes.Take(2);
            mappedType2.Name.ShouldBe($"IModel{typeToMap2.Name}");
            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

    }
}
