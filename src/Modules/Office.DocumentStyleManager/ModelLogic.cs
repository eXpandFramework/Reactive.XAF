using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager{
	public interface IModelOfficeDocumentStyleManager : IModelNode{
		IModelDocumentStyleManager DocumentStyleManager{ get; }
	}

    public static class ModelOfficeDocumentStyleManager{
        internal static IModelDocumentStyleManager DocumentStyleManager(this IModelApplication application)
            => application.ToReactiveModule<IModelReactiveModuleOffice>().Office.DocumentStyleManager();

        [PublicAPI]
        public static IObservable<IModelDocumentStyleManager> DocumentStyleManager(this IObservable<IModelOffice> source) 
            => source.Select(modules => modules.DocumentStyleManager());

        public static IModelDocumentStyleManager DocumentStyleManager(this IModelOffice office) 
            => ((IModelOfficeDocumentStyleManager) office).DocumentStyleManager;
    }

    [PublicAPI]
    public interface IModelDocumentStyleManager : IModelNode{
        [DataSourceProperty(nameof(DocumentProviders))]
        [Category("DefaultPropertiesProvider")][Required]
        IModelClass DefaultPropertiesProvider{ get; set; }
        [CriteriaOptions(nameof(DefaultPropertiesProvider)+".TypeInfo")]
        [Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" +
                XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Category("DefaultPropertiesProvider")]
        string DefaultPropertiesProviderCriteria{ get; set; }
        
        [DataSourceProperty(nameof(DefaultPropertiesProviderMembers))]
        [Category("DefaultPropertiesProvider")][Required]
        IModelMember DefaultPropertiesProviderMember{ get; set; }
        [Browsable(false)]
        IEnumerable<IModelMember> DefaultPropertiesProviderMembers{ get; }

        [Browsable(false)]
        IEnumerable<IModelClass> DocumentProviders{ get; }

        IModelImportStyles ImportStyles{ get; }

        IModelApplyTemplateListViews ApplyTemplateListViews{ get; }
        IModelDesignTemplateDetailViews DesignTemplateDetailViews{ get; }
    }

    [DomainLogic(typeof(IModelDocumentStyleManager))]
    public static class ModelOfficeModuleLogic{
	    [UsedImplicitly]
        public static IModelList<IModelClass> Get_DocumentProviders(this IModelDocumentStyleManager modelDocumentStyleManager) 
            => modelDocumentStyleManager.Application.DocumentModelClasses();

        internal static CalculatedModelNodeList<IModelClass> DocumentModelClasses(this IModelApplication application) 
            => new CalculatedModelNodeList<IModelClass>(application.BOModel
		        .Where(m => m.AllMembers.Any(member => member.Type==typeof(byte[]))&&!m.TypeInfo.Type.IsFromDocumentStyleManager()));

        [UsedImplicitly]
        public static IModelClass Get_DefaultPropertiesProvider(this IModelDocumentStyleManager modelDocumentStyleManager) 
            => modelDocumentStyleManager.ImportStyles.Select(item => item.ModelClass).FirstOrDefault();
        
        [UsedImplicitly]
        public static IModelMember Get_DefaultPropertiesProviderMember(this IModelDocumentStyleManager modelDocumentStyleManager){
	        var modelImportStylesItem = modelDocumentStyleManager.ImportStyles.FirstOrDefault(item => item.ModelClass==modelDocumentStyleManager.DefaultPropertiesProvider);
	        return modelImportStylesItem != null ? modelImportStylesItem.Member : modelDocumentStyleManager.DefaultPropertiesProvider.DocumentModelMembers().FirstOrDefault();
        }

        [UsedImplicitly]
        public static IModelList<IModelMember> Get_DefaultPropertiesProviderMembers(this IModelDocumentStyleManager modelDocumentStyleManager) 
            => modelDocumentStyleManager.DefaultPropertiesProvider.DocumentModelMembers();

        internal static IModelList<IModelMember> DocumentModelMembers(this IModelClass modelClass) 
            => new CalculatedModelNodeList<IModelMember>(modelClass == null
		        ? Enumerable.Empty<IModelMember>() : modelClass.AllMembers.Where(item => item.Type == typeof(byte[])));

        internal static IObservable<T> DefaultPropertiesProvider<T>(this XafApplication application,Func<Document,IObservable<T>> factory){
            var documentStyleManager = application.Model.DocumentStyleManager();
	        using var objectSpace = application.CreateObjectSpace(documentStyleManager.DefaultPropertiesProvider.TypeInfo.Type);
	        var theObject = objectSpace.GetObjects(documentStyleManager.DefaultPropertiesProvider.TypeInfo.Type, objectSpace.ParseCriteria(documentStyleManager.DefaultPropertiesProviderCriteria)).Cast<object>().FirstOrDefault();
	        var value = (byte[])documentStyleManager.DefaultPropertiesProviderMember.MemberInfo.GetValue(theObject);
	        return Observable.Using(() => new RichEditDocumentServer(), server => factory(RichEditDocumentServerExtensions.LoadDocument(server, value)));
        }

    }

    public interface IModelImportStyles:IModelList<IModelImportStylesItem>,IModelNode{
        [DataSourceProperty(nameof(ImportStyleItems))]
	    IModelImportStylesItem CurrentItem{ get; set; }
        [Browsable(false)]
        IEnumerable<IModelImportStylesItem> ImportStyleItems{ get; }
    }

    [DomainLogic(typeof(IModelImportStyles))]
    [UsedImplicitly]
    public class ModelImportStylesDomainLogic{
	    [UsedImplicitly]
	    public static IModelImportStylesItem Get_CurrentItem(IModelImportStyles importStyles) => importStyles.FirstOrDefault();

	    [UsedImplicitly]
	    public static IModelList<IModelImportStylesItem> Get_ImportStyleItems(IModelImportStyles importStyles) => importStyles;
    }

    [KeyProperty(nameof(ModelClassId))][PublicAPI]
    public interface IModelImportStylesItem:IModelNode{
        [Browsable(false)]
        string ModelClassId{ get; set; }
	    [DataSourceProperty(nameof(DocumentProviders))]
	    [Category("ImportStyles")]
	    [Required][RefreshProperties(RefreshProperties.All)]
	    IModelClass ModelClass{ get; set; }
	    [CriteriaOptions(nameof(ModelClass)+".TypeInfo")]
	    [Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" +
	            XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
	    [Category("ImportStyles")]
	    string Criteria{ get; set; }
	    [Required]
	    [DataSourceProperty(nameof(Members))]
	    [Category("ImportStyles")]
	    IModelMember Member{ get; set; }
	    [Browsable(false)]
	    IEnumerable<IModelMember> Members{ get; }
	    [Browsable(false)]
	    IEnumerable<IModelClass> DocumentProviders{ get; }
        [Localizable(true)]
	    string Caption{ get; set; }
    }

    [DomainLogic(typeof(IModelImportStylesItem))]
    public static class ModelImportStylesItemDomainLogic{
	    [UsedImplicitly]
	    public static string Get_Caption(IModelImportStylesItem item) => item.ModelClass?.Caption;

	    [UsedImplicitly]
	    public static IModelClass Get_ModelClass(IModelImportStylesItem item) => ((IModelDocumentStyleManager) item.Parent.Parent)
		    .DocumentProviders.FirstOrDefault(modelClass => modelClass.Id()==item.ModelClassId);

	    [UsedImplicitly]
	    public static void Set_ModelClass(IModelImportStylesItem item, IModelClass modelClass) => item.ModelClassId = modelClass.Id();

	    [UsedImplicitly]
	    public static IModelMember Get_Member(this IModelImportStylesItem item) => item
		    .ModelClass?.AllMembers.FirstOrDefault(member => member.Type == typeof(byte[]));

	    [UsedImplicitly]
	    public static IModelList<IModelClass> Get_DocumentProviders(this IModelImportStylesItem item) =>
		    item.Application.DocumentModelClasses();

	    [UsedImplicitly]
	    public static IModelList<IModelMember> Get_Members(this IModelImportStylesItem item) => item
		    .ModelClass.DocumentModelMembers();
    }
    public interface IModelApplyTemplateListViews:IModelList<IModelApplyTemplateListViewItem>,IModelNode{
    }

    [KeyProperty(nameof(ListViewId))][PublicAPI]
    public interface IModelApplyTemplateListViewItem:IModelNode{
        [Browsable(false)]
        string ListViewId{ get; set; }
        [DataSourceProperty(nameof(ListViews))][Required][RefreshProperties(RefreshProperties.All)]
        IModelListView ListView{ get; set; }
        [DataSourceProperty(nameof(TimeStamps))]
        IModelMember TimeStamp{ get; set; }
        [Required][DataSourceProperty(nameof(Contents))]
        IModelMember Content{ get; set; }
        [Browsable(false)]
        IEnumerable<IModelMember> Contents{ get; }
        [Browsable(false)]
        IEnumerable<IModelMember> TimeStamps{ get; }
        
        [Required][DataSourceProperty(nameof(DefaultMembers))]
        IModelMember DefaultMember{ get; set; }
        [Browsable(false)]
        IEnumerable<IModelMember> DefaultMembers{ get; }
        [Browsable(false)]
        IEnumerable<IModelListView> ListViews{ get; }
    }

    public interface IModelDesignTemplateDetailViews:IModelList<IModelDesignTemplateDetailView>,IModelNode{
    }

    [KeyProperty(nameof(DetailViewId))][PublicAPI]
    public interface IModelDesignTemplateDetailView:IModelNode{
        [Browsable(false)]
        string DetailViewId{ get; set; }
        [DataSourceProperty(nameof(DetailViews))][Required][RefreshProperties(RefreshProperties.All)]
        IModelDetailView DetailView{ get; set; }
        [Browsable(false)]
        IEnumerable<IModelDetailView> DetailViews{ get; }
        IModelDesignTemplateContentEditors ContentEditors{ get;  }
    }

    [DomainLogic(typeof(IModelDesignTemplateDetailView))]
    public static class ModelDesignTemplateDetailViewLogic{
        [UsedImplicitly]
        public static IModelDetailView Get_DetailView(IModelDesignTemplateDetailView view) 
            => view.DetailViews.FirstOrDefault(viewItem => viewItem.Id==view.DetailViewId);

        [UsedImplicitly]
        public static void Set_DetailView(IModelDesignTemplateDetailView view, IModelDetailView modelDetailView) 
            => view.DetailViewId = modelDetailView.Id;

        public static CalculatedModelNodeList<IModelDetailView> Get_DetailViews(IModelDesignTemplateDetailView modelDesignTemplateDetailView) 
            => modelDesignTemplateDetailView.Application.Views.OfType<IModelDetailView>()
                .Where(view => view.MemberViewItems(typeof(IRichTextPropertyEditor)).Any() && !view.ModelClass.TypeInfo.Type.IsFromDocumentStyleManager())
                .ToCalculatedModelNodeList();

        internal static bool IsFromDocumentStyleManager(this Type type) 
            => new[]{typeof(ApplyTemplateStyle), typeof(BusinessObjects.DocumentStyleManager)}.Contains(type);
    }

    public interface IModelDesignTemplateContentEditors:IModelList<IModelDesignTemplateContentEditor>,IModelNode{
    }

    [KeyProperty(nameof(ContentEditorId))][PublicAPI]
    public interface IModelDesignTemplateContentEditor:IModelNode{
        [Browsable(false)]
        string ContentEditorId{ get; set; }
        [DataSourceProperty(nameof(ContentEditors))][Required][RefreshProperties(RefreshProperties.All)]
        IModelPropertyEditor ContentEditor{ get; set; }
        [Browsable(false)]
        IEnumerable<IModelPropertyEditor> ContentEditors{ get; }
    }

    [DomainLogic(typeof(IModelDesignTemplateContentEditor))]
    public class ModelDesignTemplateContentEditorLogic{
        public CalculatedModelNodeList<IModelPropertyEditor> Get_ContentEditors(IModelDesignTemplateContentEditor modelDesignTemplateContentEditor)
            => modelDesignTemplateContentEditor.GetParent<IModelDesignTemplateDetailView>().DetailView
                .MemberViewItems(typeof(IRichTextPropertyEditor)).Cast<IModelPropertyEditor>()
                .ToCalculatedModelNodeList();

        [UsedImplicitly]
        public static IModelPropertyEditor Get_ContentEditor(IModelDesignTemplateContentEditor editor) 
            => editor.ContentEditors.FirstOrDefault(viewItem => viewItem.Id()==editor.ContentEditorId);

        [UsedImplicitly]
        public static void Set_ContentEditor(IModelDesignTemplateContentEditor contentEditor, IModelPropertyEditor propertyEditor) 
            => contentEditor.ContentEditorId = propertyEditor.Id();
    }

    [DomainLogic(typeof(IModelApplyTemplateListViewItem))]
    public static class ModelTemplateListViewLogic{
	    [UsedImplicitly]
	    public static IModelList<IModelListView> Get_ListViews(IModelApplyTemplateListViewItem item) 
            => item.Application.Views.OfType<IModelListView>()
                .Where(view => view.ModelClass.AllMembers.Any(member => member.Type==typeof(byte[]))&&!view.ModelClass.TypeInfo.Type.IsFromDocumentStyleManager())
                .ToCalculatedModelNodeList();

	    [UsedImplicitly]
	    public static IModelMember Get_Content(IModelApplyTemplateListViewItem item) 
            => item.Get_Contents().FirstOrDefault();

	    public static IModelList<IModelMember> Get_Contents(this IModelApplyTemplateListViewItem item) => item.ListView==null ?
		    new CalculatedModelNodeList<IModelMember>() :
		    item.ListView.ModelClass.AllMembers.Where(member => member.Type==typeof(byte[])).ToCalculatedModelNodeList() ;
	    
	    [UsedImplicitly]
	    public static IModelList<IModelMember> Get_TimeStamps(this IModelApplyTemplateListViewItem item) => item.ListView==null ?
		    new CalculatedModelNodeList<IModelMember>() :
		    item.ListView.ModelClass.AllMembers.Where(member => member.Type==typeof(DateTime)).ToCalculatedModelNodeList() ;
	    
	    [UsedImplicitly]
	    public static IModelMember Get_DefaultMember(IModelApplyTemplateListViewItem item) 
            => item.Get_DefaultMembers().FirstOrDefault();

	    public static IModelList<IModelMember> Get_DefaultMembers(this IModelApplyTemplateListViewItem item) 
            => item.ListView==null ? new CalculatedModelNodeList<IModelMember>() :
                new[]{item.ListView.ModelClass.AllMembers[item.ListView.ModelClass.DefaultProperty]}.Concat(item.ListView.ModelClass.AllMembers)
                    .Distinct().ToCalculatedModelNodeList();

	    [UsedImplicitly]
	    public static IModelListView Get_ListView(IModelApplyTemplateListViewItem item) 
            => item.ListViews.FirstOrDefault(viewItem => viewItem.Id==item.ListViewId);

	    [UsedImplicitly]
	    public static void Set_ListView(IModelApplyTemplateListViewItem item, IModelListView modelListView) 
            => item.ListViewId = modelListView.Id;
    }

}