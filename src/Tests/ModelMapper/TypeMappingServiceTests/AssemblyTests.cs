using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Fasterflect;
using Shouldly;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xunit;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    [Collection(nameof(ModelMapperModule))]
    public class AssemblyTests:ModelMapperBaseTest{
        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal async Task Create_Model_Assembly_in_path_if_not_Exist(Platform platform){
            InitializeMapperService(nameof(Create_Model_Assembly_in_path_if_not_Exist),platform);
            var typeToMap = typeof(TestModelMapper);

            var mapToModel = await typeToMap.MapToModel().ModelInterfaces();

            File.Exists(mapToModel.Assembly.Location).ShouldBeTrue();
        }

        [Fact]
        public async Task Assembly_Version_Should_Match_Model_Mapper_Version(){
            InitializeMapperService(nameof(Assembly_Version_Should_Match_Model_Mapper_Version));
            var typeToMap = typeof(TestModelMapper);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelMapperVersion = typeof(ModelMapperModule).Assembly.GetName().Version;
            modelType.Assembly.GetName().Version.ShouldBe(modelMapperVersion);
        }

        [Fact]
        public async Task Do_Not_Map_If_Type_Assembly_Version_Not_Changed(){
            InitializeMapperService(nameof(Do_Not_Map_If_Type_Assembly_Version_Not_Changed));
            var mappedType = typeof(TestModelMapper);

            var mapToModel = await mappedType.MapToModel().ModelInterfaces();

            var modelMapperAttribute = mapToModel.Assembly.GetCustomAttributes(typeof(ModelMapperTypeAttribute),false)
                .OfType<ModelMapperTypeAttribute>().FirstOrDefault(attribute => attribute.MappedType==mappedType.FullName&&attribute.MappedAssemmbly==mappedType.Assembly.GetName().Name);
            modelMapperAttribute.ShouldNotBeNull();

            var version = modelMapperAttribute.AssemblyHashCode;

            mappedType.MapToModel();
            
            mapToModel = await TypeMappingService.MappedTypes;

            modelMapperAttribute = mapToModel.Assembly.GetCustomAttributes(typeof(ModelMapperTypeAttribute),false)
                .OfType<ModelMapperTypeAttribute>().First(attribute => attribute.MappedType==mappedType.FullName&&attribute.MappedAssemmbly==mappedType.Assembly.GetName().Name);
            modelMapperAttribute.AssemblyHashCode.ShouldBe(version);
        }

        [Fact()]
        public async Task Always_Map_If_Any_Type_Assembly_HashCode_Changed(){

            var name = nameof(Always_Map_If_Any_Type_Assembly_HashCode_Changed);
            var mapperService = InitializeMapperService(name);
            var dynamicType = CreateDynamicType(mapperService);
            await new[]{dynamicType,typeof(TestModelMapper)}.MapToModel().ModelInterfaces();
            InitializeMapperService($"{name}",newAssemblyName:false);
            dynamicType = CreateDynamicType(mapperService);
            var first = await new[]{dynamicType,typeof(TestModelMapper)}.MapToModel().ModelInterfaces();


            InitializeMapperService($"{name}",newAssemblyName:false);

            var dynamicType2 = CreateDynamicType(mapperService, "2.0.0.0");

            var exception = Should.Throw<Exception>(async () => await new[]{dynamicType2,typeof(TestModelMapper)}.MapToModel().ModelInterfaces());


            exception.Message.ShouldContain("CS0016");
        }

        [Fact()]
        public async Task Always_Map_If_ModelMapperModule_HashCode_Changed(){
            InitializeMapperService(nameof(Always_Map_If_ModelMapperModule_HashCode_Changed));
            var mappedType = typeof(TestModelMapper);
            var first = await mappedType.MapToModel().ModelInterfaces();
            InitializeMapperService($"{nameof(Always_Map_If_ModelMapperModule_HashCode_Changed)}",newAssemblyName:false);
            typeof(TypeMappingService).SetFieldValue("_modelMapperModuleVersion", new Version(2000,100,40));

            var exception = Should.Throw<Exception>(async () => await mappedType.MapToModel().ModelInterfaces());

            exception.Message.ShouldContain("CS0016");
        }

        [Fact()]
        public async Task Always_Map_If_ModelMapperConfiguration_Changed(){
            
            InitializeMapperService(nameof(Always_Map_If_ModelMapperConfiguration_Changed));
            var typeToMap = typeof(TestModelMapper);
            var mappedTypes = await typeToMap.MapToModel().ModelInterfaces();


            mappedTypes.ShouldNotBeNull();

            InitializeMapperService(nameof(Always_Map_If_ModelMapperConfiguration_Changed),newAssemblyName:false);

            var exception = Should.Throw<Exception>(async () => await typeToMap.MapToModel(type => new ModelMapperConfiguration(type){MapName = "changed"}).ModelInterfaces());

            exception.Message.ShouldContain("CS0016");
        }

    }

}