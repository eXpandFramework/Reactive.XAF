using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using Fasterflect;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.Compiler;
using Xpand.Extensions.StreamExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Tests.BOModel;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    public abstract class ModelMapperCommonTest:BaseTest{
        

        protected const string MMListViewNodePath = "Views/" + nameof(MM) + "_ListView";
        protected const string MMDetailViewNodePath = "Views/" + nameof(MM) + "_DetailView";
        protected const string MMDetailViewTestItemNodePath = "Views/" + nameof(MM) + "_DetailView/Items/Test";
        protected const string MMListViewTestItemNodePath = "Views/" + nameof(MM) + "_ListView/Columns/Test";
        public const string DynamicTypeName = "DynamicTypeName";

        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        protected ModelMapperCommonTest() => new ModelMapperModule();

        internal ModelMapperModule DefaultModelMapperModule(Platform platform,params ModuleBase[] modules){
            var xafApplication = platform.NewApplication<ModelMapperModule>();
            xafApplication.Modules.AddRange(modules);
            var modelMapperModule = xafApplication.AddModule<ModelMapperModule>(typeof(MM));
            xafApplication.Logon();
            using (xafApplication.CreateObjectSpace()){
            }

            return modelMapperModule;
        }

        protected static PropertyInfo[] ModelTypeProperties(Type modelType) 
            => modelType.Properties().Where(info =>!TypeMappingService.ReservedPropertyNames.Contains(info.Name) &&
                                                   info.Name!=TypeMappingService.ModelMappersNodeName).ToArray();

        private void ConfigureLayoutViewPredefinedMapService(PredefinedMap predefinedMap=PredefinedMap.LayoutView){
            if (new[]{PredefinedMap.LayoutView,PredefinedMap.LayoutViewColumn}.Contains(predefinedMap)){
                typeof(PredefinedMapService).Field("_layoutViewListEditorTypeName",Flags.Static|Flags.AnyVisibility).Set(typeof(CustomGridListEditor).FullName);
            }
        }

        internal Type CreateDynamicType(string name,string version="1.0.0.0"){
            // var compilerParameters = new CompilerParameters{
            //     CompilerOptions = "/t:library",
            //     OutputAssembly = $@"{Path.GetDirectoryName(typeof(ModelMapperModule).Assembly.Location)}\DynamicType{name}{version.Substring(0,1)}.dll"
            // };
            
            

            string code=$@"
[assembly:{typeof(AssemblyVersionAttribute).FullName}(""{version}"")]
[assembly:{typeof(AssemblyFileVersionAttribute).FullName}(""{version}"")]
public class {DynamicTypeName}{{
    public string Test{{ get; set; }}
}}
";
            using var st= CSharpSyntaxTree.ParseText(code).Compile(typeof(object).Assembly.Location,typeof(AssemblyVersionAttribute).Assembly.Location);
            var path = $@"{Path.GetDirectoryName(typeof(ModelMapperModule).Assembly.Location)}\DynamicType{name}{version.Substring(0,1)}.dll";
            st.Bytes().Save(path);
            return Assembly.LoadFrom(path).GetType(DynamicTypeName);
        }

        internal string InitializeMapperService(Platform platform=Platform.Agnostic,bool newAssemblyName=true ){
            var mapperAssemblyName = $"{GetType().Name}{TestContext.CurrentContext.Test.MethodName}{platform}".GetHashCode();
            
            if (newAssemblyName){
                TypeMappingService.ModelMapperAssemblyName = $"{Guid.NewGuid()}{mapperAssemblyName}";
            }
            var applicationPath = AppDomain.CurrentDomain.ApplicationPath();
            var files = Directory.GetFiles(applicationPath,"*.dll")
                .Where(s => {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(s);
                    return fileNameWithoutExtension.StartsWith("ModelMapperAssembly")||fileNameWithoutExtension.StartsWith("ModelAssembly");
                }).ToArray();
            foreach (var file in files){
                try{
                    File.Delete(file);
                }
                catch (Exception){
                    // ignored
                }
            }

            TypeMappingService.Reset(platform:platform);
            TypeMappingService.OutputAssembly =
                $@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\{Path.GetFileNameWithoutExtension(TypeMappingService.OutputAssembly)}.dll";
            ConfigureLayoutViewPredefinedMapService();
            return mapperAssemblyName.ToString();
        }
    }

}