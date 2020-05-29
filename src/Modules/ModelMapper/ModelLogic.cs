using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.FunctionOperators;
using Xpand.Extensions.XAF.ModelExtensions;

namespace Xpand.XAF.Modules.ModelMapper{
    [DomainLogic(typeof(IModelNodeDisabled))][UsedImplicitly]
    public class ModelNodeEnabledDomainLogic{
        [UsedImplicitly]
        public static IModelObjectView Get_ParentObjectView(IModelNodeDisabled modelNodeDisabled){
            return modelNodeDisabled.GetParent<IModelObjectView>();
        }
    }

    public interface IModelNodeDisabled : IModelNode {
        [Category("Activation")]
        bool NodeDisabled { get; set; }
        [Browsable(false)][UsedImplicitly]
        IModelObjectView ParentObjectView { get; }
    }

    public interface IModelApplicationModelMapper{
        IModelModelMapper ModelMapper{ get; }         
    }

    static class ModelImageSource{
        public const string ModelMappers = "ModelEditor_Group";
        public const string ModelModelMapper = "ModelEditor_ModelMerge";
        public const string ModelModelMapperContexts = "Context_Menu_Show_In_Popup";
    }

    [ImageName(ModelImageSource.ModelModelMapper)]
    [Description("Xpand.XAF.Modules.ModelMapper settings")]
    public interface IModelModelMapper:IModelNode{
        IModelModelMapperContexts MapperContexts { get; }
    }

    [ModelNodesGenerator(typeof(ModelMapperContextNodeGenerator))]
    [ImageName(ModelImageSource.ModelModelMapperContexts)]
    [Description("Configure maps that can be shared across the model")]
    public interface IModelModelMapperContexts:IModelList<IModelModelMappers>,IModelNode{

    }

    [DomainLogic(typeof (IModelModelMapperContexts))][UsedImplicitly]
    public static class ModelModelMapperContextsDomainLogic {
        [UsedImplicitly]
        public static IEnumerable<T> GetMappers<T>(this IModelModelMapperContexts contexts) where T:IModelModelMap{
            var modelMappers = contexts.SelectMany(mappers => mappers);
            return modelMappers.Where(modelMapper => !modelMapper.NodeDisabled).OfType<T>();
        }
    }

    public class ModelMapperVisibilityCalculator:IModelIsVisible{
        public bool IsVisible(IModelNode node, string propertyName){
            var criteria = node.GetPropertyAttribute<ModelMapperBrowsableAttribute>(propertyName).Criteria;
            if (criteria != null){
                node=node.GetNode(propertyName);
                var criteriaOperator = CriteriaOperator.Parse(criteria);
                var expressionEvaluator = new ExpressionEvaluator(new EvaluatorContextDescriptorDefault(node.GetType()),criteriaOperator,customFunctions:CustomFunctions );
                var isVisible = expressionEvaluator.Evaluate(node);
                if (isVisible!=null){
                    return  (bool) isVisible;
                }
            }

            return true;
        }

        public ICollection<ICustomFunctionOperator> CustomFunctions{ get; }=new List<ICustomFunctionOperator>(){new IsAssignableFromOperator(),new PropertyExistsOperator()};
    }

    public class ModelMapperBrowsableAttribute:ModelBrowsableAttribute {
        public ModelMapperBrowsableAttribute(Type visibilityCalculatorType,string criteria,string validCriteria=null) : base(visibilityCalculatorType){
            Criteria = criteria;
            ValidCriteria = validCriteria;
        }

        public string Criteria{ get; }
        public string ValidCriteria{ get; }
    }

    
    [ImageName(ModelImageSource.ModelModelMapper)]
    public interface IModelMapperContextContainer:IModelNode{
        [DataSourceProperty("Application."+nameof(IModelApplicationModelMapper.ModelMapper)+"."+nameof(IModelModelMapper.MapperContexts))]
        [Required][UsedImplicitly]
        IModelModelMappers Context{ get; set; }
    }

    [DomainLogic(typeof(IModelMapperContextContainer))][UsedImplicitly]
    public class ModelMapperContextContainerDomainLogic{
        [UsedImplicitly]
        public static IModelModelMappers Get_Context(IModelMapperContextContainer modelModelMappers){
            var id = modelModelMappers.Id();

            return ((IModelApplicationModelMapper) modelModelMappers.Application).ModelMapper.MapperContexts[id];
        }
    }

    public class ModelMapperContextNodeGenerator:ModelNodesGeneratorBase{
        public const string Default = "Default";
        public const string ModelMapperAttribute = "ModelMapperAttribute";
        protected override void GenerateNodesCore(ModelNode node){
            if (node is IModelModelMapperContexts){
                AddNodes<IModelModelMappers>(node);
            }
            else{
                AddNodes<IModelMapperContextContainer>(node);
            }
        }

        private static void AddNodes<T>(ModelNode node) where T:IModelNode{
            node.AddNode<T>(Default);
            node.AddNode<T>(ModelMapperAttribute);
        }
    }

    [ModelNodesGenerator(typeof(ModelMappersNodeGenerator))]
    [ImageName(ModelImageSource.ModelMappers)]
    public interface IModelModelMappers:IModelList<IModelModelMap>,IModelNode{
         
    }

    public interface IModelModelMappersContextDependency:IModelNode{
        [UsedImplicitly]
        [DataSourceProperty("Application."+nameof(IModelApplicationModelMapper.ModelMapper)+"."+nameof(IModelModelMapper.MapperContexts))]
        [Category(ModelMapperModule.ModelCategory)]
        IModelModelMappers ModelMapperContext{ get; set; }
    }

    [DomainLogic(typeof(IModelModelMappersContextDependency))]
    public static class ModelModelMappersContextDependencyLogic{
        
        [UsedImplicitly]
        public static IModelModelMappers Get_ModelMapperContext(IModelModelMappersContextDependency modelMappersContextDependency){
            return ((IModelApplicationModelMapper) modelMappersContextDependency.Application).ModelMapper.MapperContexts[ModelMapperContextNodeGenerator.Default];
        }
    }
    
    public interface IModelModelMap:IModelNodeDisabled{
    }

    public interface IModelModelMapContainer:IModelNode{
        
    }

    public class ModelMappersNodeGenerator:ModelNodesGeneratorBase{
        protected override void GenerateNodesCore(ModelNode node){
            var modelMapperTypeInfos = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants .Where(info
                => info.FindAttribute<ModelAbstractClassAttribute>(false) == null && info.IsInterface);

            if (node.Id == ModelMapperContextNodeGenerator.Default){
                foreach (var info in modelMapperTypeInfos){
                    AddNode(node, info);
                }
            }
            
        }

        private void AddNode(ModelNode node, ITypeInfo typeInfo){
            var name = GetName(typeInfo);
            node.AddNode(name, typeInfo.Type);
        }

        private static string GetName(ITypeInfo typeInfo){
            var displayNameAttribute = typeInfo.FindAttribute<ModelDisplayNameAttribute>(false);
            return displayNameAttribute != null ? displayNameAttribute.ModelDisplayName : typeInfo.Type.Name.Replace("IModel", "");
        }
    }

    public static class ModelLayoutGroupLogic{
        [UsedImplicitly]
        public static string Get_Caption(IModelLayoutGroup layoutGroup){
            var count = layoutGroup.Count(_ => !(_ is IModelModelMap));
            if (count == 1 && layoutGroup[0] is IModelLayoutViewItem){
                IModelViewItem layoutItemModel = ((IModelLayoutViewItem) layoutGroup[0]).ViewItem;
                if (layoutItemModel != null){
                    return layoutItemModel.Caption;
                }
            }

            return layoutGroup.Id;
        }
    }

}