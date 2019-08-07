using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using DevExpress.ExpressApp.Model;
using EnumsNET;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.Predefined{
    
    public abstract class RepositoryItemBaseMap{
        
    }

    public abstract class PropertyEditorControlMap{
    }

    public class ViewItemService{
        public static string RepositoryItemsMapName = "RepositoryItems";
        public static string PropertyEditorControlMapName = "Controls";

        public static IObservable<Unit> Connect(){
            var repositoryItemTypes = Enums.GetValues<PredefinedMap>().Where(map => map.IsRepositoryItem())
                .Select(map => map.TypeToMap()).Where(type => type!=null)
                .ToArray();
            var propertyEditorControlTypes = Enums.GetValues<PredefinedMap>()
                .Where(map => map.IsPropertyEditor())
                .Select(map => map.TypeToMap()).Where(type => type!=null)
                .ToArray();
            return ConnectViewItemService(repositoryItemTypes, typeof(RepositoryItemBaseMap),RepositoryItemsMapName)
                .Merge(ConnectViewItemService(propertyEditorControlTypes, typeof(PropertyEditorControlMap),PropertyEditorControlMapName));
        }

        private static IObservable<Unit> Connect((Type typeToMap, string mapName, Type[] viewItemTypes) info){
            
            var genericList = typeof(IList<>).MakeGenericType(info.typeToMap);
            TypeMappingService.TypeMappingRules.Add((info.typeToMap.Name,type => ViewItem(type,info.typeToMap,info.viewItemTypes)));
            
            TypeMappingService.ContainerMappingRules.Add((info.typeToMap.Name,tuple => ViewItem(info.typeToMap,tuple,info.mapName)));
            TypeMappingService.AdditionalTypesList.Add(genericList);    
            
            return Unit.Default.AsObservable();
        }


        private static void ViewItem(ModelMapperType modelMapperType, Type typeToMap, Type[] viewItemTypes){
            if (modelMapperType.Type == typeof(RepositoryItemBaseMap)||modelMapperType.Type == typeof(PropertyEditorControlMap)){
                if (modelMapperType.CustomAttributeDatas.All(data => data.AttributeType != typeof(ModelAbstractClassAttribute))){
                    modelMapperType.CustomAttributeDatas.Add(new ModelMapperCustomAttributeData(typeof(ModelAbstractClassAttribute)));
                }
                
            }

            if (viewItemTypes.Contains(modelMapperType.Type)) {
                modelMapperType.BaseTypeFullNames.RemoveAll(s => new[]{typeof(IModelModelMapContainer).FullName}.Contains(s));
                if (modelMapperType.TypeToMap != null){
                    var modelMapName = typeToMap.ModelTypeName(typeToMap);
                    modelMapperType.BaseTypeFullNames.Add(modelMapName);

                    modelMapperType.CustomAttributeDatas.Add(new ModelMapperCustomAttributeData(typeof(ModelMapLinkAttribute),
                        new CustomAttributeTypedArgument($"{modelMapperType.Type.AssemblyQualifiedName}")));
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
            if (modelMapperType.TypeToMap==null&&viewItemTypes.Contains(modelMapperType.Type)){
                if (modelMapperType.BaseTypeFullNames.Contains(typeof(IModelModelMapContainer).FullName)){
                    modelMapperType.BaseTypeFullNames.Remove(typeof(IModelModelMapContainer).FullName);
                    modelMapperType.AdditionalPropertiesCode = null;
                }
            }
        }

        private static void ViewItem(Type typeToMap, (Type typeToMap, Result<(string key, string code)> data) data,string mapName){
            if (data.typeToMap == typeToMap){
                var result = data.data;
                var modelMapTypeName = typeToMap.ModelTypeName(typeToMap);
                string code=$"{modelMapTypeName}s {mapName}{{get;}}";
                result.Data=(mapName,code);
            }
        }


        private static IObservable<Unit> ConnectViewItemService(Type[] viewItemTypes, Type typeToMap,string mapName){
            return TypeMappingService.MappingTypes.Where(_ => viewItemTypes.Contains(_.TypeToMap)).FirstOrDefaultAsync().WhenNotDefault()
                .Select(unit => Connect((typeToMap, mapName,viewItemTypes)))
                .Switch().FirstOrDefaultAsync();
        }
    }
}