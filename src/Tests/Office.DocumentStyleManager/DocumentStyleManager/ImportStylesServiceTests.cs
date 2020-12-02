using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Moq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using DocumentFormat = DevExpress.XtraRichEdit.DocumentFormat;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.DocumentStyleManager{
    public class ImportStylesServiceTests:BaseTests{
	    private void ModelSetup(XafApplication application){
            var documentStyleManager = application.Model.DocumentStyleManager();
	        var item = documentStyleManager.ImportStyles.AddNode<IModelImportStylesItem>();
	        item.ModelClass = application.Model.BOModel.GetClass(typeof(DataObject));
        }

        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void Action_should_be_inactive_when_ImportStylesListView_is_not_set(){
            using var application=DocumentStyleManagerModule().Application;
	        ModelSetup(application);
	        var documentStyleManager = application.Model.DocumentStyleManager();
	        documentStyleManager.ImportStyles.ClearNodes();
         
            var tuple = application.SetDocumentStyleManagerDetailView(Document);
         
            var action = tuple.window.Action<DocumentStyleManagerModule>().ImportStyles();
         
            var name = nameof(ImportStylesService);
            action.Active.GetKeys().ShouldContain(name);
            action.Active[name].ShouldBe(false);
        }

        [Test][Apartment(ApartmentState.STA)][Order(1)][XpandTest()]
        public void When_Execute_Show_DocumentStyle_ListView_In_New_Modal_Window(){
	        using var application=DocumentStyleManagerModule().Application;
	        ModelSetup(application);
            var tuple = application.SetDocumentStyleManagerDetailView(Document);

            var action = tuple.window.Action<DocumentStyleManagerModule>().ImportStyles();
            action.WhenExecuted().FirstAsync().Do(_ => _.ShowViewParameters.TargetWindow=TargetWindow.NewWindow).Test();
            var testObserver = application.WhenViewOnFrame(typeof(DocumentStyle),ViewType.ListView,Nesting.Root).Select(frame => frame.View).Test();
            action.DoTheExecute();
            
            testObserver.ItemCount.ShouldBe(1);
            testObserver.Items.First().Id.ShouldBe(application.FindListViewId(typeof(DocumentStyle)));
        }

        [Test][Order(2)][XpandTest()][Apartment(ApartmentState.STA)]
        public void DocumentStyle_ListView_Contains_all_styles_from_model_ImportStylesClass_datasource(){
	        using var application=DocumentStyleManagerModule().Application;
	        ModelSetup(application);
            using (var objectSpace = application.CreateObjectSpace()){
                var dataObject = objectSpace.CreateObject<DataObject>();
                Document.NewDocumentStyle(1, DocumentStyleType.Paragraph);
                dataObject.Content=Document.ToByteArray(DocumentFormat.OpenXml);
                dataObject = objectSpace.CreateObject<DataObject>();
                RichEditDocumentServer.CreateNewDocument();
                RichEditDocumentServer.Document.NewDocumentStyle(2, DocumentStyleType.Paragraph);
                RichEditDocumentServer.Document.NewDocumentStyle(1, DocumentStyleType.Character);
                dataObject.Content=RichEditDocumentServer.Document.ToByteArray(DocumentFormat.OpenXml);
                RichEditDocumentServer.CreateNewDocument();
                dataObject = objectSpace.CreateObject<DataObject>();
                RichEditDocumentServer.Document.NewDocumentStyle(2, DocumentStyleType.Character);
                dataObject.Content=RichEditDocumentServer.Document.ToByteArray(DocumentFormat.OpenXml);
                objectSpace.CommitChanges();
            }

            
            var view = (ListView)application.NewView(application.FindListViewId(typeof(DocumentStyle)));
            var listViewFrame = application.WhenViewOnFrame(typeof(DocumentStyle)).Test();

            application.CreateViewWindow().SetView(view);

            var documentStyles = listViewFrame.Items.First().View.AsListView().CollectionSource.Objects<DocumentStyle>().ToArray();
            documentStyles.Length.ShouldBe(8);
            documentStyles.Count(style => style.DocumentStyleType==DocumentStyleType.Paragraph).ShouldBe(3);
            documentStyles.Count(style => style.DocumentStyleType==DocumentStyleType.Character).ShouldBe(5);
        }

        [Test][Order(3)][Apartment(ApartmentState.STA)][XpandTest()]
        public void When_accept_add_selected_styles_as_unused(){
	        using var application=DocumentStyleManagerModule().Application;
	        ModelSetup(application);
            IDocumentStyle documentStyle;
            using (var objectSpace = application.CreateObjectSpace()){
                var dataObject = objectSpace.CreateObject<DataObject>();
                documentStyle=Document.NewDocumentStyle(1, DocumentStyleType.Paragraph).First().ToDocumentStyle(Document);
                
                dataObject.Content=Document.ToByteArray(DocumentFormat.OpenXml);
                objectSpace.CommitChanges();
            }
            Document.DeleteStyles(documentStyle);
            var richEditServer = application.WhenDetailViewCreated().SelectMany(_ => _.e.View.WhenRichEditDocumentServer<BusinessObjects.DocumentStyleManager>(manager => manager.Content)).Test();
            var tuple = application.SetDocumentStyleManagerDetailView(Document);
            var listViewFrame = application.WhenViewOnFrame(typeof(DocumentStyle),ViewType.ListView,Nesting.Root).Test();
            var action = tuple.window.Action<DocumentStyleManagerModule>().ImportStyles();
            action.WhenExecuted().FirstAsync().Do(_ => _.ShowViewParameters.TargetWindow = TargetWindow.NewWindow).Test();
            action.DoTheExecute();

            var acceptAction = listViewFrame.Items.First().GetController<DialogController>().AcceptAction;
            var mock = new Mock<ISelectionContext>();
            mock.SetupGet(context => context.SelectedObjects).Returns(() => new[]{documentStyle});
            acceptAction.SelectionContext = mock.Object;
                
            acceptAction.DoExecute();

            richEditServer.Items.First().Document.UnusedStyles().ShouldContain(documentStyle);

        }

        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void FilterImportStyles_Should_Be_inactive_by_Default(){
	        using var application=DocumentStyleManagerModule().Application;
	        ModelSetup(application);
	        var window = application.CreateViewWindow();
	        window.SetView(application.NewView(ViewType.ListView, typeof(DocumentStyle)));
	        var filterImportStyles = window.Action<DocumentStyleManagerModule>().FilterImportStyles();

	        var boolList = filterImportStyles.Active;

            boolList.GetKeys().ShouldContain(nameof(ImportStylesService));
            boolList[nameof(ImportStylesService)].ShouldBeFalse();
        }

        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void FilterImportStyles_Items_Should_Contain_Model_ImportStyleItems(){
            using var application=DocumentStyleManagerModule().Application;
	        ModelSetup(application);
	        var window = application.CreateViewWindow();
	        window.SetView(application.NewView(ViewType.ListView, typeof(DocumentStyle)));
	        var filterImportStyles = window.Action<DocumentStyleManagerModule>().FilterImportStyles();

	        filterImportStyles.Controller.Active.Clear();

            filterImportStyles.Items.Count.ShouldBe(2);
            filterImportStyles.Items.Any(item => item.Data==application.Model.DocumentStyleManager().ImportStyles.First()).ShouldBeTrue();
            filterImportStyles.Items.Any(item => item.Data==application.Model.DocumentStyleManager().ImportStyles.Last()).ShouldBeTrue();
        }

        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void FilterImportStyles_remembers_last_selection(){
            using var application=DocumentStyleManagerModule().Application;
	        ModelSetup(application);
	        var modelImportStyles = application.Model.DocumentStyleManager().ImportStyles;
	        
	        var importStylesItem = modelImportStyles.AddNode<IModelImportStylesItem>();
	        importStylesItem.ModelClass = application.Model.BOModel.GetClass(typeof(DataObjectParent));
	        var window = application.CreateViewWindow();
	        window.SetView(application.NewView(ViewType.ListView, typeof(DocumentStyle)));
	        var filterImportStyles = window.Action<DocumentStyleManagerModule>().FilterImportStyles();
            filterImportStyles.Active.Clear();
         
            filterImportStyles.SelectedItem.Data.ShouldBe(modelImportStyles.CurrentItem);
         
            filterImportStyles.SelectedItem = filterImportStyles.Items.First(item => item.Data == importStylesItem);
            modelImportStyles.CurrentItem.ShouldBe(importStylesItem);
        }

        [Test][Apartment(ApartmentState.STA)][XpandTest()]
        public void FilterImportStyles_Action_Should_Be_Active_In_Import_Styles_ListView(){
	        using var application=DocumentStyleManagerModule().Application;
	        ModelSetup(application);
	        var tuple = application.SetDocumentStyleManagerDetailView(Document);
	        var action = tuple.window.Action<DocumentStyleManagerModule>().ImportStyles();
	        action.WhenExecuted().FirstAsync().Do(_ => _.ShowViewParameters.TargetWindow = TargetWindow.NewWindow).Test();
	        var listViewFrame = application.WhenViewOnFrame(typeof(DocumentStyle),ViewType.ListView,Nesting.Root).Test();
	        action.DoExecute();

	        var filterImportStyles = listViewFrame.Items.First().Action<DocumentStyleManagerModule>().FilterImportStyles();
	        filterImportStyles.Active[nameof(ImportStylesService)].ShouldBe(true);
        }

        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void When_FilterImportStyles_Selection_Changed_Update_ImportedStyles_ListView(){
            using var application=DocumentStyleManagerModule().Application;
	        ModelSetup(application);
	        var modelImportStyles = application.Model.DocumentStyleManager().ImportStyles;
	        modelImportStyles.CurrentItem = null;
	        var window = application.CreateViewWindow();
	        window.SetView(application.NewView(ViewType.ListView, typeof(DocumentStyle)));
	        var filterImportStyles = window.Action<DocumentStyleManagerModule>().FilterImportStyles();
	        filterImportStyles.Controller.Active.Clear();
	        
	        var testObserver = ((NonPersistentObjectSpace) filterImportStyles.View().AsListView().ObjectSpace).WhenObjectsGetting().Test();
         
	        filterImportStyles.SelectedItem = filterImportStyles.Items.Last();
         
            testObserver.ItemCount.ShouldBe(1);
        }

        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void ImportStyles_initial_loading_Respects_FilterImportStyles_CurrentValue(){
	        using var application=DocumentStyleManagerModule().Application;
	        ModelSetup(application);
	        var window = application.CreateViewWindow();
	        var filterImportStyles = window.Action<DocumentStyleManagerModule>().FilterImportStyles();
	        filterImportStyles.Active.Clear();
	        window.SetView(application.NewView(ViewType.ListView, typeof(DocumentStyle)));
	        
        }

    }
}