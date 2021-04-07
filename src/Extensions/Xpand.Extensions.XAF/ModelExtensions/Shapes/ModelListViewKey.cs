using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Xpand.Extensions.XAF.ModelExtensions.Shapes {
    [KeyProperty(nameof(ListViewId))]
    [ModelDisplayName(nameof(ListView))]
    public interface IModelListViewKey : IModelNode {
        [Required]
        [DataSourceProperty(nameof(ListViews))]
        IModelListView ListView{ get; set; }
        [Browsable(false)]
        string ListViewId { get; set; }
        [Browsable(false)]
        IModelList<IModelListView> ListViews{ get; }
    }

    
    public interface IModelListViewKeys:IModelList<IModelListViewKey>,IModelNode{
    }


    [DomainLogic(typeof(IModelListViewKey))]
    public class ModelListViewKeyLogic{
        public static IModelListView Get_ListView(IModelListViewKey feature) =>!string.IsNullOrEmpty(feature.ListViewId)? (IModelListView) feature.Application.Views[feature.ListViewId]:null;

        public static void Set_ListView(IModelListViewKey listViewFeature, IModelListView listView) => listViewFeature.ListViewId = listView?.Id;

        public static IModelList<IModelListView> Get_ListViews(IModelListViewKey modelListViewFeature) 
            => modelListViewFeature.Application.Views.OfType<IModelListView>().ToCalculatedModelNodeList();
    }

    public interface IModelListViewColumns:IModelNode,IModelList<IModelListViewColumn> { }

    [KeyProperty(nameof(Key))]
    public interface IModelListViewColumn : IModelNode {
        [Browsable(false)]
        string Key { get; set; }
        [Required][DataSourceProperty(nameof(ListViews))]
        IModelListView ListView { get; set; }
        [Browsable(false)]
        IModelList<IModelListView> ListViews{ get; }
        [DataSourceProperty(nameof(ListView)+".Columns")]
        [Required]
        IModelColumn Column { get; set; }
    }

    [DomainLogic(typeof(IModelListViewColumn))]
    public class ModelListViewColumnLogic{
        public static IModelListView Get_ListView(IModelListViewColumn listViewColumn) 
            =>!string.IsNullOrEmpty(listViewColumn.Key)? (IModelListView) listViewColumn.Application.Views[listViewColumn.Key.Split('-')[0]]:null;
        
        public static IModelColumn Get_Column(IModelListViewColumn listViewColumn) 
            =>listViewColumn.ListView?.Columns[listViewColumn.Key.Split('-')[1]];

        public static void Set_ListView(IModelListViewColumn listViewColumn, IModelListView listView) 
            => listViewColumn.Key = $"{listView.Id}-{listViewColumn.Column?.Id}";
        
        public static void Set_Column(IModelListViewColumn listViewColumn, IModelColumn modelColumn) 
            => listViewColumn.Key = $"{listViewColumn.ListView?.Id}-{modelColumn.Id}";

        public static IModelList<IModelListView> Get_ListViews(IModelListViewColumn modelListViewColumn) 
            => modelListViewColumn.Application.Views.OfType<IModelListView>().ToCalculatedModelNodeList();
    }

}
