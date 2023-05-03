using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.Persistent.Base;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;

using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    [NonParallelizable]
    public class ContainerTests:ModelMapperCommonTest{
        [Test]
        
        public async Task Custom_Container_Image(){
            InitializeMapperService();
            var typeToMap = typeof(TestModelMapper);
            var imageName = "ImageName";

            var modelType = await new ModelMapperConfiguration(typeToMap){ImageName = imageName}.MapToModel()
                .ModelInterfaces();

            var imageNameAttribute = modelType.Attribute<ImageNameAttribute>();
            imageNameAttribute.ShouldNotBeNull();
            imageNameAttribute.ImageName.ShouldBe(imageName);
        }


        [Test]
        
        public async Task Container_Interface(){
            InitializeMapperService();
            var typeToMap = typeof(TestModelMapper);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var containerType = modelType.Assembly.GetType($"IModel{typeToMap.Name}{TypeMappingService.DefaultContainerSuffix}");
            containerType.ShouldNotBeNull();
            var propertyInfo = containerType.GetProperty($"{typeToMap.Name}");
            propertyInfo.ShouldNotBeNull();
            propertyInfo.CanWrite.ShouldBeFalse();
            propertyInfo.PropertyType.Name.ShouldBe(typeToMap.ModelTypeName());
        }

        [Test]
        
        public async Task Custom_Container_Name(){
            InitializeMapperService();
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

        [Test]
        
        public async Task ModelMappers_Interface(){
            InitializeMapperService();
            var typeToMap = typeof(TestModelMapper);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var containerName = nameof(TestModelMapper);
            var containerType = modelType.Assembly.GetType($"IModel{containerName}{TypeMappingService.DefaultContainerSuffix}");
            
            var propertyInfo = containerType.GetProperty(containerName)?.PropertyType.GetProperty(TypeMappingService.ModelMappersNodeName);
            propertyInfo.ShouldNotBeNull();
            propertyInfo.CanWrite.ShouldBeFalse();

        }
    }
}
