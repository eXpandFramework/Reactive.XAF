using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.GridListEditor{
    
    public interface IModelReactiveModuleGridListEditor:IModelReactiveModule{
        IModelGridListEditor GridListEditor{ get; }
    }

    public interface IModelGridListEditor:IModelNode{
        IModelGridListEditorRules GridListEditorRules{ get;  }
    }

    public static class ModelReactiveModuleGridListEditor{
        public static IObservable<IModelGridListEditorRule> Rules(this IObservable<IModelGridListEditor> source) 
            => source.SelectMany(editor => editor.GridListEditorRules);

        public static IObservable<IModelGridListEditor> GridListEditor(this IObservable<IModelReactiveModules> source) 
            => source.Select(modules => modules.GridListEditor());

        public static IModelGridListEditor GridListEditor(this IModelReactiveModules reactiveModules) 
            => ((IModelReactiveModuleGridListEditor) reactiveModules).GridListEditor;
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
        [DataSourceProperty("Application.Views")]
        [DataSourceCriteria("(AsObjectView Is Not Null)")]
        IModelListView ListView{ get; set; }
    }

    [ModelDisplayName("RememberTopRow")]
    public interface IModelGridListEditorTopRow:IModelGridListEditorRule{
        
    }

    [ModelDisplayName("FocusRow")]
    public interface IModelGridListEditorFocusRow:IModelGridListEditorRule{
        [DataSourceProperty(nameof(RowHandles))]
        string RowHandle { get; set; }
        [DataSourceProperty(nameof(RowHandles))]
        string UpArrowMoveToRowHandle { get; set; }
        [Browsable(false)]
        IEnumerable<string> RowHandles { get; }
    }

    [DomainLogic(typeof(IModelGridListEditorFocusRow))]
    public static class GridListEditorFocusRow {
        
        public static IEnumerable<string> Get_RowHandles(IModelGridListEditorFocusRow focusRow) 
            => AppDomain.CurrentDomain.GridControlHandles().Select(info => info.Name);

        internal static IEnumerable<FieldInfo> GridControlHandles(this AppDomain appDomain) 
            => appDomain.GetAssemblyType("DevExpress.XtraGrid.GridControl").Fields(Flags.StaticPublic)
                .Where(info => info.Name.EndsWith("Handle"));
    }
}