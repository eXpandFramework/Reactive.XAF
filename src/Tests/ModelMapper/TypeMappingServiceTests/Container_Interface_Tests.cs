using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.Persistent.Base;
using Fasterflect;
using Shouldly;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xunit;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    
    public partial class ObjectMappingServiceTests{
        [Fact]
        public async Task Custom_Container_Image(){
            InitializeMapperService(nameof(Custom_Container_Image));
            var typeToMap = typeof(TestModelMapper);
            var imageName = "ImageName";

            var modelType = await new ModelMapperConfiguration(typeToMap){ImageName = imageName}.MapToModel()
                .ModelInterfaces();

            var imageNameAttribute = modelType.Attribute<ImageNameAttribute>();
            imageNameAttribute.ShouldNotBeNull();
            imageNameAttribute.ImageName.ShouldBe(imageName);
        }


        [Fact]
        public async Task Container_Interface(){
            InitializeMapperService(nameof(Container_Interface));
            var typeToMap = typeof(TestModelMapper);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var containerType = modelType.Assembly.GetType($"IModel{typeToMap.Name}{TypeMappingService.DefaultContainerSuffix}");
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

            var modelType = await new ModelMapperConfiguration(typeToMap){ContainerName = containerName, MapName = mapName}
                .MapToModel()
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
            var containerType = modelType.Assembly.GetType($"IModel{containerName}{TypeMappingService.DefaultContainerSuffix}");
            
            var propertyInfo = containerType.GetProperty(containerName)?.PropertyType.GetProperty(TypeMappingService.ModelMappersNodeName);
            propertyInfo.ShouldNotBeNull();
            propertyInfo.CanWrite.ShouldBeFalse();

        }
    }
}
