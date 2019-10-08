using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.RefreshView{
    
    public interface IModelReactiveModuleRefreshView:IModelReactiveModule{
        IModelRefreshView RefreshView{ get; }
    }

    public interface IModelRefreshView:IModelNode{
        IModelRefreshViewItems Items{ get; }
    }

    public static class ModelRefreshView{
        public static IObservable<IModelRefreshView> RefreshViewModel(this IObservable<IModelReactiveModules> source){
            return source.Select(modules => modules.RefreshView());
        }

        public static IModelRefreshView RefreshView(this IModelReactiveModules reactiveModules){
            return ((IModelReactiveModuleRefreshView) reactiveModules).RefreshView;
        }

    }

    [ModelNodesGenerator(typeof(ModelRefreshViewsNodesGenerator))]
    public interface IModelRefreshViewItems:IModelNode,IModelList<IModelRefreshViewItem>{
    }

    public class ModelRefreshViewsNodesGenerator:ModelNodesGeneratorBase{
        protected override void GenerateNodesCore(ModelNode node){
            
        }
    }


    public interface IModelRefreshViewItem:IModelNode{
        [DataSourceProperty("Application.Views")]
        IModelView View{ get; set; }
        TimeSpan Interval{ get; set; }
    }
}