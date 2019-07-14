using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.Predifined{
    
    public abstract class RepositoryItemBase{
        
    }
    public class RepositoryItemService{
        public static string MapName = "RepositoryItems";
        private static IEnumerable<Type> _predifinedTypes;

        public static IObservable<Unit> Connect(){
            _predifinedTypes = EnumsNET.Enums.GetValues<PredifinedMap>().Where(map => map.IsRepositoryItem()).Select(map => map.GetTypeToMap());
            var typeToMap = typeof(RepositoryItemBase);
            var genericList = typeof(IList<>).MakeGenericType(typeof(RepositoryItemBase));
            TypeMappingService.TypeMappingRules.Add((nameof(RepositoryItem),type => RepositoryItem(type,typeToMap)));
            
            TypeMappingService.ContainerMappingRules.Add((nameof(RepositoryItem),tuple => RepositoryItem(typeToMap,tuple)));
            TypeMappingService.AdditionalTypesList.Add(genericList);
            return Unit.Default.AsObservable();
        }


        private static void RepositoryItem(ModelMapperType modelMapperType, Type typeToMap){
            
            if (_predifinedTypes.Select(type => $"IModel{type.Name}").Contains(modelMapperType.ModelName)) {
                modelMapperType.BaseTypeFullNames.RemoveAll(s => new[]{typeof(IModelModelMapContainer).FullName}.Contains(s));
                if (modelMapperType.TypeToMap == null){
//                    modelMapperType.BaseTypeFullNames.RemoveAll(s => new[]{typeof(IModelModelMap).FullName}.Contains(s));
                }
                else{
                    var modelMapName = typeof(RepositoryItemBase).ModelMapName(typeToMap);
                    modelMapperType.BaseTypeFullNames.Add(modelMapName);
                    modelMapperType.BaseTypeFullNames.Add(typeof(IModelNode).FullName);
                }

                if (modelMapperType.AdditionalPropertiesCode != null){
                    modelMapperType.AdditionalPropertiesCode = null;        
                }
                
            }
            else if (typeToMap == modelMapperType.Type){
                if (modelMapperType.AdditionalPropertiesCode != null){
                    modelMapperType.AdditionalPropertiesCode = null;        
                }
                
            }
            if (modelMapperType.TypeToMap==null&&_predifinedTypes.Contains(modelMapperType.Type)){
                if (modelMapperType.BaseTypeFullNames.Contains(typeof(IModelModelMapContainer).FullName)){
                    modelMapperType.BaseTypeFullNames.Remove(typeof(IModelModelMapContainer).FullName);
                }
            }
            
            

            
        }

        private static void RepositoryItem(Type typeToMap, (Type typeToMap, Result<(string key, string code)> data) data){
            if (data.typeToMap == typeToMap){
                var result = data.data;
                var modelMapName = typeof(RepositoryItemBase).ModelMapName(typeToMap);
                string code=$"{modelMapName}s {MapName}{{get;}}";
                result.Data=(MapName,code);
            }
        }
    }
}