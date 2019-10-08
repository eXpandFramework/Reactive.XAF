using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.GridListEditor{
    
    public interface IModelReactiveModuleGridListEditor:IModelReactiveModule{
        IModelGridListEditor GridListEditor{ get; }
    }

    public interface IModelGridListEditor:IModelNode{
        IModelGridListEditorRules GridListEditorRules{ get;  }
    }

    public static class ModelReactiveModuleGridListEditor{
        public static IObservable<IModelGridListEditorRule> Rules(this IObservable<IModelGridListEditor> source){
            return source.SelectMany(editor => editor.GridListEditorRules);
        }

        public static IObservable<IModelGridListEditor> GridListEditor(this IObservable<IModelReactiveModules> source){
            return source.Select(modules => modules.GridListEditor());
        }

        public static IModelGridListEditor GridListEditor(this IModelReactiveModules reactiveModules){
            return ((IModelReactiveModuleGridListEditor) reactiveModules).GridListEditor;
        }

    }

    [ModelNodesGenerator(typeof(ModelGridListEditorRulesNodesGenerator))]
    public interface IModelGridListEditorRules:IModelList<IModelGridListEditorRule>,IModelNode{
    }

    public class ModelGridListEditorRulesNodesGenerator:ModelNodesGeneratorBase{
        protected override void GenerateNodesCore(ModelNode node){
            
        }
    }

    [ModelAbstractClass]
    public interface IModelGridListEditorRule:IModelNode{
    }

    [ModelDisplayName("RememberTopRow")]
    
    public interface IModelGridListEditorTopRow:IModelGridListEditorRule{
        [DataSourceProperty("Application.Views")]
        [DataSourceCriteria("(AsObjectView Is Not Null)")]
        IModelListView ListView{ get; set; }
    }
}