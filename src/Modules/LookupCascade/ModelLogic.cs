using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.LookupCascade{
    [PublicAPI]
    public interface IModelReactiveModuleLookupCascade:IModelReactiveModule{
        IModelLookupCascade LookupCascade{ get; }
    }
    
    public static class ModelLookupCascade{
        public static IObservable<IModelLookupCascade> LookupCascadeModel(this IObservable<IModelReactiveModules> source) 
            => source.Select(modules => modules.LookupCascade());

        public static IModelLookupCascade LookupCascade(this IModelReactiveModules reactiveModules) 
            => ((IModelReactiveModuleLookupCascade) reactiveModules).LookupCascade;
    }

    [PublicAPI]
    public interface IModelLookupCascade:IModelNode{
        IModelClientDatasource ClientDatasource{ get;  }
    }

    [PublicAPI]
    public interface IModelClientDatasourceLookupView:IModelNode{
        [DataSourceProperty(nameof(LookupListViews))]
        [Required][Description("Lists ListViews that use the "+nameof(ASPxLookupCascadePropertyEditor))]
        IModelListView LookupListView{ get; [UsedImplicitly] set; }
        [Browsable(false)]
        IEnumerable<IModelListView> LookupListViews{ [UsedImplicitly] get; }
    }

    [DomainLogic(typeof(IModelClientDatasourceLookupView))]
    public static class ModelClientDatasourceLookupViewLogic{
        [UsedImplicitly]
        public static IModelList<IModelListView> Get_LookupListViews(IModelClientDatasourceLookupView lookupView) 
            => lookupView.Application.Views.OfType<IModelObjectView>()
                .SelectMany(view => view.MemberViewItems(typeof(ASPxLookupCascadePropertyEditor)))
                .Select(item => item.GetLookupListView())
                .Distinct().ToCalculatedModelNodeList();
    }

    public interface IModelClientDatasourceLookupViews:IModelList<IModelClientDatasourceLookupView>,IModelNode{
    }

    [PublicAPI]
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
        public bool IsVisible(IModelNode node, string propertyName) 
            => typeof(ASPxLookupCascadePropertyEditor).IsAssignableFrom(((IModelMemberViewItem) node).PropertyEditorType);
    }

    [ModelAbstractClass]
    public interface IModelColumnClientVisible:IModelColumn{
        [Category(LookupCascadeModule.ModuleName)]
        bool? ClientVisible{ get; set; }    
    }

    [PublicAPI]
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

        [Browsable(false)]
        IEnumerable<IModelColumn> CascadeColumnFilters{ [UsedImplicitly] get; }
        [Browsable(false)]
        IEnumerable<IModelMemberViewItem> LookupPropertyEditorMemberViewItems{ [UsedImplicitly] get; }
    }


    [DomainLogic(typeof(IModelLookupCascadePropertyEditor))]
    [UsedImplicitly]
    public class ModelLookupCasadePropertyEditorLogic{

        [UsedImplicitly]
        public static IModelList<IModelColumn> Get_CascadeColumnFilters(IModelLookupCascadePropertyEditor modelLookupPropertyEditor){
            var cascadeMemberViewItem = modelLookupPropertyEditor.CascadeMemberViewItem;
            var keyMemberMemberType = modelLookupPropertyEditor.GetParent<IModelMemberViewItem>().ModelMember.MemberInfo.MemberTypeInfo.KeyMember.MemberType;
            return new CalculatedModelNodeList<IModelColumn>(cascadeMemberViewItem != null
                ? cascadeMemberViewItem.GetLookupListView().VisibleMemberViewItems().Cast<IModelColumn>().Where(_ => _.ModelMember.Type==keyMemberMemberType)
                : Enumerable.Empty<IModelColumn>());
        }

        [UsedImplicitly]
        public static IModelList<IModelMemberViewItem> Get_LookupPropertyEditorMemberViewItems(IModelLookupCascadePropertyEditor modelLookupPropertyEditor) 
            => modelLookupPropertyEditor.GetParent<IModelObjectView>().MemberViewItems()
                .Where(item => item != modelLookupPropertyEditor.Parent && typeof(ASPxLookupCascadePropertyEditor).IsAssignableFrom(item.PropertyEditorType)).ToCalculatedModelNodeList();
    }


}
