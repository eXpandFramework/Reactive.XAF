using System;
using System.IO;
using System.Linq;
using Fasterflect;
using Tests.Artifacts;
using Tests.Modules.ModelMapper.BOModel;
using Xpand.Source.Extensions.System.AppDomain;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper;

namespace Tests.Modules.ModelMapper{
    public abstract class ModelMapperBaseTest:BaseTest{
        internal ModelMapperModule DefaultModelMapperModule(Platform platform){
            return platform.NewApplication().AddModule<ModelMapperModule>(typeof(MM));
        }

        internal void InitializeMapperService(string modelMapperAssemblyName,Platform platform=Platform.Agnostic){
            var mapperAssemblyName = $"{GetType().Name}{modelMapperAssemblyName}{platform}".GetHashCode();
            ModelMapperService.ModelMapperAssemblyName = $"{Guid.NewGuid():N}{mapperAssemblyName}";
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
            typeof(ModelMapperService).CallMethod(null, "Init");
            typeof(ModelMapperService).SetFieldValue("_modelMapperModuleVersion", typeof(ModelMapperModule).Assembly.GetName().Version);
        }
    }

}