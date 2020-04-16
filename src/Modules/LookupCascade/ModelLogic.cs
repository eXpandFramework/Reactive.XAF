using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.Model;

namespace Xpand.XAF.Modules.LookupCascade{
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
                .SelectMany(view => view.MemberViewItems(typeof(ASPxLookupCascadePropertyEditor)))
                .Select(item => item.GetLookupListView())
                .Distinct();
            return new CalculatedModelNodeList<IModelListView>(modelListViews);
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
    public interface IModelMemberViewItemLookupCascadePropertyEditor:IModelMemberViewItem{
        [ModelBrowsable(typeof(ModelMemberViewItemLooupCascadePropertyEditorVisibilityCalculator))]
        IModelLookupCascadePropertyEditor LookupCascade{ get; }
    }

    public class ModelMemberViewItemLooupCascadePropertyEditorVisibilityCalculator:IModelIsVisible{
        public bool IsVisible(IModelNode node, string propertyName){
            return typeof(ASPxLookupCascadePropertyEditor).IsAssignableFrom(((IModelMemberViewItem) node).PropertyEditorType);
        }
    }

    [ModelAbstractClass]
    public interface IModelColumnClientVisible:IModelColumn{
        bool? ClientVisible{ get; set; }    
    }

    
    public interface IModelLookupCascadePropertyEditor:IModelNode{
        [DefaultValue("{0}")]
        string TextFormatString{ get; [UsedImplicitly] set; }
        
        [DataSourceProperty(nameof(LookupPropertyEditorMemberViewItems))]
        [Category("Cascade")]
        IModelMemberViewItem CascadeMemberViewItem{ get; set; }
        
        [RuleRequiredField(TargetCriteria = nameof(CascadeMemberViewItem )+" Is Not Null")]
        [DataSourceProperty(nameof(CascadeColumnFilters))]
        [Category("Cascade")]
        [Description("Lists memberViewItme of the " +nameof(CascadeMemberViewItem)+ " LookupView Members of the same type to current memberViewItem key type. Only visible columns are listed, to hide the column on the client false the "+nameof(IModelColumnClientVisible.ClientVisible)+" on the lookup view")]
        IModelColumn CascadeColumnFilter{ get; [UsedImplicitly] set; }
        [Category("Synchronize")]
        bool Synchronize{ get; set; }
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


    [DomainLogic(typeof(IModelLookupCascadePropertyEditor))]
    [UsedImplicitly]
    public class ModelLookupCasadePropertyEditorLogic{
        [UsedImplicitly]
        public static IModelList<IModelColumn> Get_SynchronizeMemberLookupColumns(IModelLookupCascadePropertyEditor modelLookupPropertyEditor){
            var viewItem = modelLookupPropertyEditor.SynchronizeMemberViewItem;
            return new CalculatedModelNodeList<IModelColumn>(viewItem!=null?viewItem.GetLookupListView().VisibleMemberViewItems().Cast<IModelColumn>():Enumerable.Empty<IModelColumn>());
        }

        [UsedImplicitly]
        public static IModelList<IModelColumn> Get_CascadeColumnFilters(IModelLookupCascadePropertyEditor modelLookupPropertyEditor){
            var cascadeMemberViewItem = modelLookupPropertyEditor.CascadeMemberViewItem;
            return new CalculatedModelNodeList<IModelColumn>(cascadeMemberViewItem != null
                ? cascadeMemberViewItem.GetLookupListView().VisibleMemberViewItems().Cast<IModelColumn>()
                    .Where(_ => _.ModelMember.Type==modelLookupPropertyEditor.GetParent<IModelMemberViewItem>().ModelMember.MemberInfo.Owner.KeyMember.MemberType)
                : Enumerable.Empty<IModelColumn>());
        }

        [UsedImplicitly]
        public static IModelList<IModelMemberViewItem> Get_LookupPropertyEditorMemberViewItems(IModelLookupCascadePropertyEditor modelLookupPropertyEditor){
            var modelMemberViewItems = modelLookupPropertyEditor.GetParent<IModelObjectView>().MemberViewItems()
                .Where(item => item != modelLookupPropertyEditor.Parent && typeof(ASPxLookupCascadePropertyEditor).IsAssignableFrom(item.PropertyEditorType));
            return new CalculatedModelNodeList<IModelMemberViewItem>(modelMemberViewItems);
        }
    }


}
