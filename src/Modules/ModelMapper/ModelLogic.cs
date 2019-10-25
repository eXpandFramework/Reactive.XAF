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
using Xpand.Extensions.XAF.FunctionOperators;
using Xpand.Extensions.XAF.Model;

namespace Xpand.XAF.Modules.ModelMapper{
    [DomainLogic(typeof(IModelNodeDisabled))]
    public class ModelNodeEnabledDomainLogic{
        public static IModelObjectView Get_ParentObjectView(IModelNodeDisabled modelNodeDisabled){
            return modelNodeDisabled.GetParent<IModelObjectView>();
        }
    }

    public interface IModelNodeDisabled : IModelNode {
        [Category("Activation")]
        bool NodeDisabled { get; set; }
        [Browsable(false)]
        IModelObjectView ParentObjectView { get; }
    }

    public interface IModelApplicationModelMapper{
        IModelModelMapper ModelMapper{ get; }         
    }

    class ModelImageSource{
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

    [DomainLogic(typeof (IModelModelMapperContexts))]
    public static class ModelModelMapperContextsDomainLogic {
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
        [Required]
        IModelModelMappers Context{ get; set; }
    }

    [DomainLogic(typeof(IModelMapperContextContainer))]
    public class ModelMapperContextContainerDomainLogic{
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

    public interface IModelModelMap:IModelNodeDisabled{
    }

    public interface IModelModelMapContainer:IModelNode{
        
    }

    public class ModelMappersNodeGenerator:ModelNodesGeneratorBase{
        protected override void GenerateNodesCore(ModelNode node){
            var modelMapperTypeInfos = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants.Where(info
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