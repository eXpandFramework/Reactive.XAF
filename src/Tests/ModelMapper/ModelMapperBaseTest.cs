using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using Fasterflect;
using Microsoft.CSharp;
using TestsLib;
using Xpand.Source.Extensions.System.AppDomain;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Tests.BOModel;
using Xunit.Abstractions;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    public abstract class ModelMapperBaseTest:BaseTest{
        

        protected const string MMListViewNodePath = "Views/" + nameof(MM) + "_ListView";
        public const string DynamicTypeName = "DynamicTypeName";
        internal ModelMapperModule DefaultModelMapperModule(Platform platform,params ModuleBase[] modules){
            var xafApplication = platform.NewApplication();
            xafApplication.Modules.AddRange(modules);
            return xafApplication.AddModule<ModelMapperModule>(typeof(MM));
        }

        protected static PropertyInfo[] ModelTypeProperties(Type modelType){
            return modelType.Properties().Where(info =>!TypeMappingService.ReservedPropertyNames.Contains(info.Name) &&
                                                       info.Name!=TypeMappingService.ModelMappersNodeName).ToArray();
        }

        private void ConfigureLayoutViewPredifinedMapService(PredifinedMap predifinedMap=PredifinedMap.LayoutView){
            if (new[]{PredifinedMap.LayoutView,PredifinedMap.LayoutViewColumn}.Contains(predifinedMap)){
                typeof(PredifinedMapService).Field("_xpandWinAssembly",Flags.Static|Flags.AnyVisibility).Set(GetType().Assembly);
                typeof(PredifinedMapService).Field("_layoutViewListEditorTypeName",Flags.Static|Flags.AnyVisibility).Set(typeof(CustomGridListEditor).FullName);
            }
        }

        internal Type CreateDynamicType(string name,string version="1.0.0.0"){
            var compilerParameters = new CompilerParameters{
                CompilerOptions = "/t:library",
                OutputAssembly = $@"{Path.GetDirectoryName(typeof(ModelMapperModule).Assembly.Location)}\DynamicType{name}{version.Substring(0,1)}.dll"
            };
            
            var codeProvider = new CSharpCodeProvider();
            compilerParameters.ReferencedAssemblies.Add(typeof(object).Assembly.Location);
            compilerParameters.ReferencedAssemblies.Add(typeof(AssemblyVersionAttribute).Assembly.Location);

            string code=$@"
[assembly:{typeof(AssemblyVersionAttribute).FullName}(""{version}"")]
[assembly:{typeof(AssemblyFileVersionAttribute).FullName}(""{version}"")]
public class {DynamicTypeName}{{
    public string Test{{ get; set; }}
}}
";
            var compilerResults = codeProvider.CompileAssemblyFromSource(compilerParameters, code);
            return compilerResults.CompiledAssembly.GetType(DynamicTypeName);
        }

        internal string InitializeMapperService(string modelMapperAssemblyName,Platform platform=Platform.Agnostic,bool newAssemblyName=true ){

            TypeMappingService.PropertyMappingRules.Clear();
            typeof(ModelExtendingService).SetPropertyValue("Platform", platform);
            var mapperAssemblyName = $"{GetType().Name}{modelMapperAssemblyName}{platform}".GetHashCode();
            if (newAssemblyName){
                TypeMappingService.ModelMapperAssemblyName = $"{Guid.NewGuid():N}{mapperAssemblyName}";
            }
            var applicationPath = AppDomain.CurrentDomain.ApplicationPath();
            var files = Directory.GetFiles(applicationPath,$"*{mapperAssemblyName}*.dll").ToArray();
            foreach (var file in files){
                try{
                    File.Delete(file);
                }
                catch (Exception){
                    // ignored
                }
            }
            typeof(TypeMappingService).CallMethod(null, "Init");
            typeof(PredifinedMapService).CallMethod(null, "Init");
            typeof(TypeMappingService).SetFieldValue("_modelMapperModuleVersion", typeof(ModelMapperModule).Assembly.GetName().Version);
            ConfigureLayoutViewPredifinedMapService();
            return mapperAssemblyName.ToString();
        }
    }

}