using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.XtraGrid.Views.Grid;
using Fasterflect;
using Shouldly;
using Xpand.Source.Extensions.System.String;
using Xpand.XAF.Modules.ModelMapper;
using Xunit;

namespace Tests.Modules.ModelMapper.ModelMapperService{
    
    public partial class ModelMapperServiceTests{
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
                modelProperty.PropertyType.Name.ShouldBe($"IModel{propertyInfo.PropertyType.FullName.CleanCodeName()}");
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
        [InlineData(typeof(GridView))]
        public async Task Map_DevExpress_Types(Type typeToMap){
            InitializeMapperService(nameof(Map_DevExpress_Types));

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            modelType.Name.ShouldBe($"IModel{typeToMap.Name}");
            
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
        public async Task Map_Multiple_Objects_from_the_different_subscription_In_the_same_assembly(){
            var typeToMap1 = typeof(TestModelMapper);
            var typeToMap2 = typeof(StringValueTypeProperties);
            InitializeMapperService(nameof(Map_Multiple_Objects_from_the_different_subscription_In_the_same_assembly));

            await new[]{typeToMap1}.MapToModel();
            await new[]{typeToMap2}.MapToModel();

            Xpand.XAF.Modules.ModelMapper.ModelMapperService.Start();
            var mappedType1 = await Xpand.XAF.Modules.ModelMapper.ModelMapperService.MappedTypes.Take(1);
            mappedType1.Name.ShouldBe($"IModel{typeToMap1.Name}");
            var mappedType2 = await Xpand.XAF.Modules.ModelMapper.ModelMapperService.MappedTypes.Take(2);
            mappedType2.Name.ShouldBe($"IModel{typeToMap2.Name}");
            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

    }
}
