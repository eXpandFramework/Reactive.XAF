using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Fasterflect;
using Shouldly;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.ObjectMapping;
using Xunit;

namespace Tests.Modules.ModelMapper.TypeMappingServiceTests{
    [Collection(nameof(XafTypesInfo))]
    public partial class ObjectMappingServiceTests{

        [Fact]
        public async Task Assembly_Version_Should_Match_Model_Mapper_Version(){
            InitializeMapperService(nameof(Assembly_Version_Should_Match_Model_Mapper_Version));
            var typeToMap = typeof(TestModelMapper);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelMapperVersion = typeof(ModelMapperModule).Assembly.GetName().Version;
            modelType.Assembly.GetName().Version.ShouldBe(modelMapperVersion);
        }
        
        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal async Task Create_Model_Assembly_in_path_if_not_Exist(Platform platform){
            InitializeMapperService(nameof(Create_Model_Assembly_in_path_if_not_Exist),platform);
            var typeToMap = typeof(TestModelMapper);

            var mapToModel = await typeToMap.MapToModel().ModelInterfaces();

            File.Exists(mapToModel.Assembly.Location).ShouldBeTrue();
        }

        [Fact()]
        public async Task Always_Map_If_Any_Type_Assembly_Version_Changed(){
            var name = nameof(Always_Map_If_Any_Type_Assembly_Version_Changed);
            var mapperService = InitializeMapperService(name);

            var dynamicType = CreateDynamicType(mapperService);

            await new[]{typeof(TestModelMapper),dynamicType}.MapToModel().ModelInterfaces();
            InitializeMapperService($"{name}",newAssemblyName:false);

            var dynamicType2 = CreateDynamicType(mapperService, "2.0.0.0");
            
            var exception = Should.Throw<Exception>(async () => await new[]{typeof(TestModelMapper),dynamicType2}.MapToModel().ModelInterfaces());

            exception.Message.ShouldStartWith("error CS0016: Could not write to output file");
        }

        [Fact()]
        public async Task Always_Map_If_ModelMapperModule_Version_Changed(){
            InitializeMapperService(nameof(Always_Map_If_ModelMapperModule_Version_Changed));
            var mappedType = typeof(TestModelMapper);
            await mappedType.MapToModel().ModelInterfaces();
            InitializeMapperService($"{nameof(Always_Map_If_ModelMapperModule_Version_Changed)}",newAssemblyName:false);
            typeof(TypeMappingService).SetFieldValue("_modelMapperModuleVersion", new Version(2000,100,40));

            var exception = Should.Throw<Exception>(async () => await mappedType.MapToModel().ModelInterfaces());

            exception.Message.ShouldStartWith("error CS0016: Could not write to output file");
        }

        [Fact()]
        public async Task Always_Map_If_ModelMapperConfiguration_Changed(){
            
            InitializeMapperService(nameof(Always_Map_If_ModelMapperConfiguration_Changed));
            var typeToMap = typeof(TestModelMapper);
            var mappedTypes = await typeToMap.MapToModel().ModelInterfaces();


            mappedTypes.ShouldNotBeNull();

            InitializeMapperService(nameof(Always_Map_If_ModelMapperConfiguration_Changed),newAssemblyName:false);

            var exception = Should.Throw<Exception>(async () => {
                await typeToMap.MapToModel(new ModelMapperConfiguration(){ContainerName = "changed"}).ModelInterfaces();
            });

            exception.Message.ShouldStartWith("error CS0016: Could not write to output file");
        }

        [Fact(Skip = NotImplemented)]
        public async Task Always_Map_If_Additional_Type_is_mapped(){
            
            InitializeMapperService(nameof(Always_Map_If_Additional_Type_is_mapped));
            
        }
    }

}