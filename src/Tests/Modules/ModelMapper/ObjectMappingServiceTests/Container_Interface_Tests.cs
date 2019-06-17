using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.Persistent.Base;
using Fasterflect;
using Shouldly;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.ObjectMapping;
using Xunit;

namespace Tests.Modules.ModelMapper.ObjectMappingServiceTests{
    
    public partial class ObjectMappingServiceTests{
        [Fact]
        public async Task Custom_Container_Image(){
            InitializeMapperService(nameof(Custom_Container_Image));
            var typeToMap = typeof(TestModelMapper);
            var codeName = typeToMap.Name;
            var imageName = "ImageName";

            var modelType = await typeToMap.MapToModel(new ModelMapperConfiguration(){ImageName = imageName})
                
                .ModelInterfaces();


            var containerType = modelType.Assembly.GetType($"IModel{codeName}{ObjectMappingService.DefaultContainerSuffix}");
            var imageNameAttribute = containerType.Attribute<ImageNameAttribute>();
            imageNameAttribute.ShouldNotBeNull();
            imageNameAttribute.ImageName.ShouldBe(imageName);
        }


        [Fact]
        public async Task Container_Interface(){
            InitializeMapperService(nameof(Container_Interface));
            var typeToMap = typeof(TestModelMapper);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var containerType = modelType.Assembly.GetType($"IModel{typeToMap.Name}{ObjectMappingService.DefaultContainerSuffix}");
            containerType.ShouldNotBeNull();
            var propertyInfo = containerType.GetProperty($"{typeToMap.Name}");
            propertyInfo.ShouldNotBeNull();
            propertyInfo.CanWrite.ShouldBeFalse();
            propertyInfo.PropertyType.Name.ShouldBe($"IModel{typeToMap.Name}");
        }

        [Fact]
        public async Task Custom_Container_Name(){
            InitializeMapperService(nameof(Custom_Container_Name));
            var typeToMap = typeof(TestModelMapper);
            var containerName = "Custom";
            string mapName="mapName";

            var modelType = await typeToMap
                .MapToModel(new ModelMapperConfiguration(){ContainerName = containerName, MapName = mapName})
                .ModelInterfaces();

            var containerType = modelType.Assembly.GetType($"IModel{containerName}");
            var propertyInfo = containerType.Properties().First();
            propertyInfo.Name.ShouldBe(mapName);
            
        }

        [Fact]
        public async Task ModelMappers_Interface(){
            InitializeMapperService(nameof(ModelMappers_Interface));
            var typeToMap = typeof(TestModelMapper);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var containerName = typeof(TestModelMapper).Name;
            var containerType = modelType.Assembly.GetType($"IModel{containerName}{ObjectMappingService.DefaultContainerSuffix}");
            
            var propertyInfo = containerType.GetProperty(containerName)?.PropertyType.GetProperty(ObjectMappingService.ModelMappersNodeName);
            propertyInfo.ShouldNotBeNull();
            propertyInfo.CanWrite.ShouldBeFalse();

        }
    }
}
