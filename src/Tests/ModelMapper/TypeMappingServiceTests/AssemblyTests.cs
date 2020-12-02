using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.Model;
using DevExpress.Web;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    [NonParallelizable]
    public class AssemblyTests:ModelMapperBaseTest{
	    
        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        [TestCase(nameof(Platform.Web))]
        public async Task Create_Model_Assembly_in_path_if_not_Exist(string platformName){
            var platform = GetPlatform(platformName);
            InitializeMapperService(platform);
            var typeToMap = typeof(TestModelMapper);

            var mapToModel = await typeToMap.MapToModel().ModelInterfaces();

            File.Exists(mapToModel.Assembly.Location).ShouldBeTrue();
        }
        [XpandTest]
        [TestCase(typeof(TestModelMapper),nameof(Platform.Win))]
        [TestCase(typeof(TestModelMapper),nameof(Platform.Web))]
        public void Platform_Detection(Type typeToMap,string platformName){
            var platform = GetPlatform(platformName);
            InitializeMapperService();

            var module = typeToMap.Extend<IModelListView>();
            using (DefaultModelMapperModule( platform, module).Application){
                typeToMap.ModelType().Assembly.Location.ShouldEndWith($"{platformName}{TypeMappingService.CustomAssemblyNameSuffix}.dll");
            }
        }

        [Test]
        [XpandTest]
        public async Task Assembly_Version_Should_Match_Model_Mapper_Version(){
            InitializeMapperService();
            var typeToMap = typeof(TestModelMapper);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelMapperVersion = typeof(ModelMapperModule).Assembly.GetName().Version;
            modelType.Assembly.GetName().Version.ShouldBe(modelMapperVersion);
        }

        [Test]
        [XpandTest]
        public async Task Do_Not_Map_If_Type_Assembly_Version_Not_Changed(){
            InitializeMapperService();
            var mappedType = typeof(TestModelMapper);

            var mapToModel = await mappedType.MapToModel().ModelInterfaces();

            var modelMapperAttribute = mapToModel.Assembly.GetCustomAttributes(typeof(ModelMapperTypeAttribute),false)
                .OfType<ModelMapperTypeAttribute>().FirstOrDefault(attribute => attribute.MappedType==mappedType.FullName&&attribute.MappedAssembly==mappedType.Assembly.GetName().Name);
            modelMapperAttribute.ShouldNotBeNull();

            var version = modelMapperAttribute.AssemblyHashCode;

            
            
            mapToModel = await TypeMappingService.MappedTypes;

            modelMapperAttribute = mapToModel.Assembly.GetCustomAttributes(typeof(ModelMapperTypeAttribute),false)
                .OfType<ModelMapperTypeAttribute>().First(attribute => attribute.MappedType==mappedType.FullName&&attribute.MappedAssembly==mappedType.Assembly.GetName().Name);
            modelMapperAttribute.AssemblyHashCode.ShouldBe(version);
        }

        [Test]
        [XpandTest]
        public async Task Always_Map_If_Any_Type_Assembly_HashCode_Changed(){

            
            var mapperService = InitializeMapperService();
            var dynamicType = CreateDynamicType(mapperService);
            await new[]{dynamicType,typeof(TestModelMapper)}.MapToModel().ModelInterfaces();
            InitializeMapperService(newAssemblyName:false);
            dynamicType = CreateDynamicType(mapperService);
            await new[]{dynamicType,typeof(TestModelMapper)}.MapToModel().ModelInterfaces();


            InitializeMapperService(newAssemblyName:false);

            var dynamicType2 = CreateDynamicType(mapperService, "2.0.0.0");

            Should.Throw<UnauthorizedAccessException>(async () => await new[]{dynamicType2,typeof(TestModelMapper)}.MapToModel().ModelInterfaces());

        }

        [Test]
        [XpandTest]
        public async Task Always_Map_If_ModelMapperModule_HashCode_Changed(){
            InitializeMapperService();
            var mappedType = typeof(TestModelMapper);
            await mappedType.MapToModel().ModelInterfaces();
            InitializeMapperService(newAssemblyName:false);
            typeof(TypeMappingService).SetFieldValue("_modelMapperModuleVersion", new Version(2000,100,40));

            Should.Throw<UnauthorizedAccessException>(async () => await mappedType.MapToModel().ModelInterfaces());
        }

        [Test]
        [XpandTest]
        public async Task Always_Map_If_ModelMapperConfiguration_Changed(){
            
            InitializeMapperService();
            var typeToMap = typeof(TestModelMapper);
            var mappedTypes = await typeToMap.MapToModel().ModelInterfaces();


            mappedTypes.ShouldNotBeNull();

            InitializeMapperService(newAssemblyName:false);

            Should.Throw<UnauthorizedAccessException>(async () => await typeToMap.MapToModel(type => new ModelMapperConfiguration(type){MapName = "changed"}).ModelInterfaces());
            
        }

        [Test]
        [XpandTest]
        public void Use_separate_assembly_for_custom_map(){
            
            InitializeMapperService();
            var typesToMap = new[] {
                typeof(TestModelMapper), 
                typeof(ASPxComboBox)
            };

            
            var observer = typesToMap.MapToModel().ModelInterfaces().Select(type => type.Assembly).Test();

            
            observer.ItemCount.ShouldBe(2);
            observer.Items.FirstOrDefault(assembly => assembly.GetName().Name.Contains("Custom")).ShouldNotBeNull();
            observer.Items.FirstOrDefault(assembly => !assembly.GetName().Name.Contains("Custom")).ShouldNotBeNull();
        }

    }

}