using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using Fasterflect;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.StyleTemplateService;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests{
    static class TestExtensions{

        public static CharacterPropertiesBase[] NewDocumentStyle(this Document document, int stylesCount,DocumentStyleType type,bool doNotApply=false) =>
            (type == DocumentStyleType.Paragraph ? document.NewParagraphStyle(doNotApply,stylesCount) : document.NewCharacterStyle(stylesCount,doNotApply)).ToArray();

        public const string PsStyleName = "MyPStyle";
        static IEnumerable<CharacterPropertiesBase> NewParagraphStyle(this Document document,bool doNotApply,int stylesCount){
            for (int i = 0; i < stylesCount; i++){
                document.BeginUpdate();
                var paragraphStyle = document.NewParagraphStyle( i,doNotApply);
                yield return paragraphStyle;
                if (i < stylesCount-1){
                    document.Paragraphs.Append();
                }
                document.EndUpdate();
            }
        }

        public static CharacterPropertiesBase NewParagraphStyle(this Document document,   int paragraph,bool doNotApply=false){
	        var documentParagraph = document.Paragraphs[paragraph];
	        document.InsertText(documentParagraph.Range.Start, $"test{paragraph}");
	        var documentStyle = new DocumentStyle(){StyleName = $"{PsStyleName}{paragraph}", FontSize = 22, FontName = "Verdana"};
	        var paragraphStyle = documentStyle.Get(document);
	        if (!doNotApply){
		        documentParagraph.Style = (ParagraphStyle) paragraphStyle;
	        }

	        return paragraphStyle;
        }

        public const string CsStyleName = "MyCStyle";
        static IEnumerable<CharacterStyle> NewCharacterStyle(this Document document,int stylesCount,bool doNotApply){
            for (int i = 0; i < stylesCount; i++){
                var documentRange = document.AppendText($"test{i}");
                var documentStyle = new DocumentStyle(){StyleName = $"{CsStyleName}{i}",FontSize = 22,FontName = "Verdana",DocumentStyleType = DocumentStyleType.Character};
                var charPropsStyle =(CharacterStyle) documentStyle.Get(document);
                document.CharacterStyles.Add(charPropsStyle);
                if (!doNotApply){
	                var charProps = document.BeginUpdateCharacters(documentRange);
	                charProps.Style = charPropsStyle;
	                document.EndUpdateCharacters(charProps);
                }
                yield return charPropsStyle;
            }
        }

        public static IObjectSpace NonPersistentObjectSpace(this XafApplication application){
            return application.CreateObjectSpace(typeof(DocumentStyle));
        }

        public static (Window window, BusinessObjects.ApplyTemplateStyle applyTemplateStyle)  SetDocumentStyleManagerDetailView(this XafApplication application){
	        var window = application.CreateViewWindow();
            var item = application.Model.DocumentStyleManager().ApplyTemplateListViews.AddNode<IModelApplyTemplateListViewItem>();
	        item.ListView = application.Model.BOModel.GetClass(typeof(DataObject)).DefaultListView;
	        window.SetView(application.NewView(ViewType.ListView, typeof(DataObject)));
	        var action = window.Action<DocumentStyleManagerModule>().ShowApplyStylesTemplate();
	        action.WhenExecuted().TakeFirst()
		        .Do(e => e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow).Test();
	        return (window, (BusinessObjects.ApplyTemplateStyle) window.View.CurrentObject);
        }
        public static (Window window, BusinessObjects.DocumentStyleManager documentStyleManager) SetDocumentStyleManagerDetailView(
            this XafApplication application, Document document, Action<Window> windowCreated = null){
            var objectSpace = application.NonPersistentObjectSpace();
            var documentStyleManager = objectSpace.CreateObject<BusinessObjects.DocumentStyleManager>();
            documentStyleManager.Content = document.ToByteArray(DocumentFormat.OpenXml);
            documentStyleManager.SynchronizeStyles();
            var window = application.CreateViewWindow();
            windowCreated?.Invoke(window);
            window.SetView(application.CreateDetailView(objectSpace, documentStyleManager));
            return (window,documentStyleManager);
        }
        public static Window ShowDocumentStyleManagerDetailView(this XafApplication application, DataObject dataObject){
            application.Model.EnableShowStyleManager();
            var window = application.CreateViewWindow();
            
            window.SetView(window.Application.CreateDetailView(dataObject));
            
            var singleChoiceAction = window.Action<DocumentStyleManagerModule>().ShowStyleManager();
            var testObserver = application.WhenWindowCreated().Test();
            singleChoiceAction.WhenExecuted().TakeFirst()
                .Do(e => e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow).Test();
            singleChoiceAction.DoExecute(singleChoiceAction.Items.First());
            var managerWindow = testObserver.Items.First();
            return managerWindow;
        }

        public static (Window window, BusinessObjects.ApplyTemplateStyle applyTemplateStyle)
	        SetApplyTemplateStyleDetailView(this XafApplication application, Document document = null){

	        document ??= new RichEditDocumentServer().Document;
	        var modelListView = application.Model.BOModel.GetClass(typeof(DataObject)).DefaultListView;
            var modelDocumentStyleManager = application.Model.DocumentStyleManager();
	        var applyTemplateListViewItem = modelDocumentStyleManager.ApplyTemplateListViews.AddNode<IModelApplyTemplateListViewItem>();
	        applyTemplateListViewItem.ListView=modelListView;
	        
	        var dataObjectSpace = application.CreateObjectSpace();
            var dataObject = dataObjectSpace.CreateObject<DataObject>();
	        dataObject.Content = document.ToByteArray(DocumentFormat.OpenXml);
	        dataObjectSpace.CommitChanges();
            var applyTemplateStyle = application.NewApplyStyleTemplate(modelListView, dataObject);
	        var detailView = application.CreateDetailView( applyTemplateStyle);
	        var window = application.CreateViewWindow();
	        window.SetView(detailView);
	        return (window,applyTemplateStyle);
        }

        

        public static void OnSelectionChanged(this IRichEditDocumentServer server){
            var documentServer = server.GetPropertyValue("InnerDocumentServer");
            documentServer.GetType().Method("OnSelectionChanged").Call(documentServer, null, null);
        }

        public static void EnableShowStyleManager(this IModelApplication application){
            var modelDocumentStyleManager = application.DocumentStyleManager();
            var modelDesignTemplateDetailView = modelDocumentStyleManager.DesignTemplateDetailViews.AddNode<IModelDesignTemplateDetailView>();
            modelDesignTemplateDetailView.DetailView = application.BOModel.GetClass(typeof(DataObject)).DefaultDetailView;
            var modelDesignTemplateContentEditor = modelDesignTemplateDetailView.ContentEditors.AddNode<IModelDesignTemplateContentEditor>();
            modelDesignTemplateContentEditor.ContentEditor = modelDesignTemplateContentEditor.ContentEditors.First();
        }
    }
}