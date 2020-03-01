using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.Model;

namespace Xpand.XAF.Modules.ClientLookupCascade{
    public interface IModelOptionsClientDatasource:IModelNode{
        IModelClientDatasource ClientDatasource{ get;  }
    }

    public interface IModelClientDatasourceLookupView:IModelNode{
        [DataSourceProperty(nameof(LookupListViews))]
        [Required]
        IModelListView LookupListView{ get; [UsedImplicitly] set; }
        [Browsable(false)]
        IEnumerable<IModelListView> LookupListViews{ [UsedImplicitly] get; }
    }

    [DomainLogic(typeof(IModelClientDatasourceLookupView))]
    public static class ModelClientDatasourceLookupViewLogic{
        [UsedImplicitly]
        public static IModelList<IModelListView> Get_LookupListViews(IModelClientDatasourceLookupView lookupView){
            var modelListViews = lookupView.Application.Views.OfType<IModelObjectView>()
                .SelectMany(view => view.MemberViewItems(typeof(ASPxClientLookupCascadePropertyEditor)))
                .Select(item => item.GetLookupListView()).OfType<IModelListView>()
                .Distinct();
            return new CalculatedModelNodeList<IModelListView>(modelListViews);
        }

        public static string GetUniqueID(this IModelListView lookupListViewModel,string parentViewId){
            return $"{lookupListViewModel.Id}__guid__{parentViewId}";
        }

    }

    public interface IModelClientDatasourceLookupViews:IModelList<IModelClientDatasourceLookupView>,IModelNode{
    }

    public interface IModelClientDatasource:IModelNode{
        ClientStorage ClientStorage{ get; [UsedImplicitly] set; }
        IModelClientDatasourceLookupViews LookupViews{ get; }
    }

    [PublicAPI]
    public enum ClientStorage{
        SessionStorage,
        LocaStorage
    }

    [ModelAbstractClass]
    public interface IModelMemberViewItemASPxClientLookupPropertyEditor:IModelMemberViewItem{
        [ModelBrowsable(typeof(ModelMemberViewItemASPxClientLooupPropertyEditorVisibilityCalculator))]
        IModelASPxClientLookupPropertyEditor ASPxClientLookupPropertyEditor{ get; }
    }

    public class ModelMemberViewItemASPxClientLooupPropertyEditorVisibilityCalculator:IModelIsVisible{
        public bool IsVisible(IModelNode node, string propertyName){
            return typeof(ASPxClientLookupCascadePropertyEditor).IsAssignableFrom(((IModelMemberViewItem) node).PropertyEditorType);
        }
    }

    [ModelAbstractClass]
    public interface IModelColumnClientVisible:IModelColumn{
        bool? ClientVisible{ get; set; }    
    }

    
    public interface IModelASPxClientLookupPropertyEditor:IModelNode{
        [DefaultValue("{0}")]
        string TextFormatString{ get; [UsedImplicitly] set; }
        
        [DataSourceProperty(nameof(LookupPropertyEditorMemberViewItems))]
        [Category("Cascade")]
        IModelMemberViewItem CascadeMemberViewItem{ get; set; }
        
        [RuleRequiredField(TargetCriteria = nameof(CascadeMemberViewItem )+" Is Not Null")]
        [DataSourceProperty(nameof(CascadeColumnFilters))]
        [Category("Cascade")]
        [Description("Column of the " +nameof(CascadeMemberViewItem)+ " View uppon which the cascaded lookup will be filtered. Only visible columns are listed, to hide the column on the client false the "+nameof(IModelColumnClientVisible.ClientVisible)+" on the lookup view")]
        IModelColumn CascadeColumnFilter{ get; [UsedImplicitly] set; }

        [DataSourceProperty(nameof(LookupPropertyEditorMemberViewItems))]
        [Category("Synchronize")]
        IModelMemberViewItem SynchronizeMemberViewItem{ get; set; }

        [Category("Synchronize")]
        [DataSourceProperty(nameof(SynchronizeMemberLookupColumns))]
        [RuleRequiredField(TargetCriteria = nameof(SynchronizeMemberViewItem)+" Is Not Null")]
        [Description("This column value will be used on the client to set the value of the " +nameof(SynchronizeMemberViewItem)+ ". Only visible columns are listed, to hide the column on the client false the "+nameof(IModelColumnClientVisible.ClientVisible)+" on the lookup view")]
        IModelColumn SynchronizeMemberLookupColumn{ get; [UsedImplicitly] set; }

        [Browsable(false)]
        IEnumerable<IModelColumn> CascadeColumnFilters{ [UsedImplicitly] get; }
        [Browsable(false)]
        IEnumerable<IModelColumn> SynchronizeMemberLookupColumns{ [UsedImplicitly] get; }
        [Browsable(false)]
        IEnumerable<IModelMemberViewItem> LookupPropertyEditorMemberViewItems{ [UsedImplicitly] get; }
    }


    [DomainLogic(typeof(IModelASPxClientLookupPropertyEditor))]
    [UsedImplicitly]
    public class ModelASPxClientLookupPropertyEditorLogic{
        [UsedImplicitly]
        public static IModelList<IModelColumn> Get_SynchronizeMemberLookupColumns(IModelASPxClientLookupPropertyEditor modelLookupPropertyEditor){
            var viewItem = modelLookupPropertyEditor.SynchronizeMemberViewItem;
            return new CalculatedModelNodeList<IModelColumn>(viewItem!=null?viewItem.GetLookupListView().VisibleMemberViewItems().Cast<IModelColumn>():Enumerable.Empty<IModelColumn>());
        }

        [UsedImplicitly]
        public static IModelList<IModelColumn> Get_CascadeColumnFilters(IModelASPxClientLookupPropertyEditor modelLookupPropertyEditor){
            var cascadeMemberViewItem = modelLookupPropertyEditor.CascadeMemberViewItem;
            return new CalculatedModelNodeList<IModelColumn>(cascadeMemberViewItem!=null?cascadeMemberViewItem.GetLookupListView().VisibleMemberViewItems().Cast<IModelColumn>():Enumerable.Empty<IModelColumn>());
        }

        [UsedImplicitly]
        public static IModelList<IModelMemberViewItem> Get_LookupPropertyEditorMemberViewItems(IModelASPxClientLookupPropertyEditor modelLookupPropertyEditor){
            var modelMemberViewItems = modelLookupPropertyEditor.GetParent<IModelObjectView>().MemberViewItems()
                .Where(item => item != modelLookupPropertyEditor.Parent && typeof(ASPxClientLookupCascadePropertyEditor).IsAssignableFrom(item.PropertyEditorType));
            return new CalculatedModelNodeList<IModelMemberViewItem>(modelMemberViewItems);
        }
    }

    public class ClientLookupModelExtender:Controller,IModelExtender{
        public void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            extenders.Add<IModelOptions,IModelOptionsClientDatasource>();
            extenders.Add<IModelMemberViewItem,IModelMemberViewItemASPxClientLookupPropertyEditor>();
            extenders.Add<IModelColumn,IModelColumnClientVisible>();
        }
    }

}
