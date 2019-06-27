using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using Fasterflect;
using Microsoft.CSharp;
using Tests.Artifacts;
using Tests.Modules.ModelMapper.BOModel;
using Xpand.Source.Extensions.System.AppDomain;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper;
using Xpand.XAF.Modules.ModelMapper.Services;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Tests.Modules.ModelMapper{
    public abstract class ModelMapperBaseTest:BaseTest{
        protected const string MMListViewNodePath = "Views/" + nameof(MM) + "_ListView";
        public const string DynamicTypeName = "DynamicTypeName";
        internal ModelMapperModule DefaultModelMapperModule(Platform platform){
            return platform.NewApplication().AddModule<ModelMapperModule>(typeof(MM));
        }

        protected static PropertyInfo[] ModelTypeProperties(Type modelType){
            return modelType.Properties().Where(info =>!TypeMappingService.ReservedPropertyNames.Contains(info.Name) &&
                                                       info.Name!=TypeMappingService.ModelMappersNodeName).ToArray();
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
            TypeMappingService.AttributeMappingRules.Clear();
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
            typeof(TypeMappingService).SetFieldValue("_modelMapperModuleVersion", typeof(ModelMapperModule).Assembly.GetName().Version);
            return mapperAssemblyName.ToString();
        }
    }

}