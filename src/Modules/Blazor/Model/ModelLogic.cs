using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Blazor.Model{
	public interface IModelReactiveModulesBlazor : IModelReactiveModule{
		IModelBlazor Blazor{ get; }
	}

    public interface IModelBlazor:IModelNode{
        IModelListViewFeatures ListViewFeatures{ get; } 
    }

    public interface IModelListViewFeatures:IModelList<IModelListViewFeature>,IModelNode{
    }

    [KeyProperty(nameof(ListViewId))]
    public interface IModelListViewFeature:IModelNode{
        [Required]
        [DataSourceProperty(nameof(ListViews))]
        IModelListView ListView{ get; set; }
        [Browsable(false)]
        string ListViewId { get; set; }
        [Browsable(false)]
        IModelList<IModelListView> ListViews{ get; }
        IModelDxDataGridModel DxDataGridModel { get; }
    }

    [DomainLogic(typeof(IModelListViewFeature))]
    public class ModelListViewFeatureLogic{
        public static IModelListView Get_ListView(IModelListViewFeature feature) =>!string.IsNullOrEmpty(feature.ListViewId)? (IModelListView) feature.Application.Views[feature.ListViewId]:null;

        public static void Set_ListView(IModelListViewFeature listViewFeature, IModelListView listView) => listViewFeature.ListViewId = listView?.Id;

        public static IModelList<IModelListView> Get_ListViews(IModelListViewFeature modelListViewFeature) 
            => modelListViewFeature.Application.Views.OfType<IModelListView>().ToCalculatedModelNodeList();
    }
    
    
}
