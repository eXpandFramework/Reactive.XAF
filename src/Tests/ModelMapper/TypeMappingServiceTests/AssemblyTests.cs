using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    [NonParallelizable]
    public class AssemblyTests:ModelMapperBaseTest{
        [XpandTimeout]
        [TestCase(nameof(Platform.Win))]
        [TestCase(nameof(Platform.Web))]
        public async Task Create_Model_Assembly_in_path_if_not_Exist(string platformName){
            var platform = GetPlatform(platformName);
            InitializeMapperService(nameof(Create_Model_Assembly_in_path_if_not_Exist),platform);
            var typeToMap = typeof(TestModelMapper);

            var mapToModel = await typeToMap.MapToModel().ModelInterfaces();

            File.Exists(mapToModel.Assembly.Location).ShouldBeTrue();
        }
        [XpandTimeout]
        [TestCase(typeof(TestModelMapper),nameof(Platform.Win))]
        [TestCase(typeof(TestModelMapper),nameof(Platform.Web))]
        public void Platform_Detection(Type typeToMap,string platformName){
            var platform = GetPlatform(platformName);
            InitializeMapperService($"{nameof(Platform_Detection)}{typeToMap.Name}{platform}");

            var module = typeToMap.Extend<IModelListView>();
            using (DefaultModelMapperModule(nameof(Platform_Detection), platform, module).Application){
                typeToMap.ModelType().Assembly.GetName().Name.ShouldEndWith(platformName);
            }
        }

        [Test]
        [XpandTimeout]
        public async Task Assembly_Version_Should_Match_Model_Mapper_Version(){
            InitializeMapperService(nameof(Assembly_Version_Should_Match_Model_Mapper_Version));
            var typeToMap = typeof(TestModelMapper);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelMapperVersion = typeof(ModelMapperModule).Assembly.GetName().Version;
            modelType.Assembly.GetName().Version.ShouldBe(modelMapperVersion);
        }

        [Test]
        [XpandTimeout]
        public async Task Do_Not_Map_If_Type_Assembly_Version_Not_Changed(){
            InitializeMapperService(nameof(Do_Not_Map_If_Type_Assembly_Version_Not_Changed));
            var mappedType = typeof(TestModelMapper);

            var mapToModel = await mappedType.MapToModel().ModelInterfaces();

            var modelMapperAttribute = mapToModel.Assembly.GetCustomAttributes(typeof(ModelMapperTypeAttribute),false)
                .OfType<ModelMapperTypeAttribute>().FirstOrDefault(attribute => attribute.MappedType==mappedType.FullName&&attribute.MappedAssemmbly==mappedType.Assembly.GetName().Name);
            modelMapperAttribute.ShouldNotBeNull();

            var version = modelMapperAttribute.AssemblyHashCode;

            
            
            mapToModel = await TypeMappingService.MappedTypes;

            modelMapperAttribute = mapToModel.Assembly.GetCustomAttributes(typeof(ModelMapperTypeAttribute),false)
                .OfType<ModelMapperTypeAttribute>().First(attribute => attribute.MappedType==mappedType.FullName&&attribute.MappedAssemmbly==mappedType.Assembly.GetName().Name);
            modelMapperAttribute.AssemblyHashCode.ShouldBe(version);
        }

        [Test]
        [XpandTimeout]
        public async Task Always_Map_If_Any_Type_Assembly_HashCode_Changed(){

            var name = nameof(Always_Map_If_Any_Type_Assembly_HashCode_Changed);
            var mapperService = InitializeMapperService(name);
            var dynamicType = CreateDynamicType(mapperService);
            await new[]{dynamicType,typeof(TestModelMapper)}.MapToModel().ModelInterfaces();
            InitializeMapperService($"{name}",newAssemblyName:false);
            dynamicType = CreateDynamicType(mapperService);
            await new[]{dynamicType,typeof(TestModelMapper)}.MapToModel().ModelInterfaces();


            InitializeMapperService($"{name}",newAssemblyName:false);

            var dynamicType2 = CreateDynamicType(mapperService, "2.0.0.0");

            var exception = Should.Throw<Exception>(async () => await new[]{dynamicType2,typeof(TestModelMapper)}.MapToModel().ModelInterfaces());


            exception.Message.ShouldContain("CS0016");
        }

        [Test]
        [XpandTimeout]
        public async Task Always_Map_If_ModelMapperModule_HashCode_Changed(){
            InitializeMapperService(nameof(Always_Map_If_ModelMapperModule_HashCode_Changed));
            var mappedType = typeof(TestModelMapper);
            await mappedType.MapToModel().ModelInterfaces();
            InitializeMapperService($"{nameof(Always_Map_If_ModelMapperModule_HashCode_Changed)}",newAssemblyName:false);
            typeof(TypeMappingService).SetFieldValue("_modelMapperModuleVersion", new Version(2000,100,40));

            var exception = Should.Throw<Exception>(async () => await mappedType.MapToModel().ModelInterfaces());

            exception.Message.ShouldContain("CS0016");
        }

        [Test]
        [XpandTimeout]
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