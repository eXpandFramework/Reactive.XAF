using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Templates.ActionContainers;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Web.SystemModule;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Web;
using DevExpress.XtraGrid;
using Moq;
using Moq.Protected;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Logger.Hub;
using Xpand.XAF.Modules.Reactive.Services;
using EditorsFactory = DevExpress.ExpressApp.Editors.EditorsFactory;

namespace TestsLib {
    public static class Extensions {

        public static async Task<T> WithTimeOut<T>(this Task<T> source, TimeSpan? timeout = null){
            return await source.ToObservable().WithTimeOut(timeout);
        }

        public static async Task<T> WithTimeOut<T>(this IObservable<T> source, TimeSpan? timeout=null){
            timeout = timeout ?? TimeSpan.FromSeconds(5);
            return await source.Timeout(timeout.Value);
        }
        public static Mock<T> GetMock<T>(this T t) where T:class{
            return Mock.Get(t);
        }

        public static void MockCreateControls(this DashboardView view) {
            foreach (var dashboardViewItem in view.GetItems<DashboardViewItem>()){
                dashboardViewItem.CreateControl();
            }
            view.CreateControls();
        }

        public static void SetupDefaults(this XafApplication application, params ModuleBase[] modules){
            application.RegisterDefaults(modules);
            application.Setup();        }

        public static void RegisterDefaults(this XafApplication application, params ModuleBase[] modules){
            application.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
            var moduleBases = new[] {
                new ReactiveLoggerHubModule(),
                new SystemModule(),
                application is WinApplication ? (ModuleBase) new SystemWindowsFormsModule() : new SystemAspNetModule()
            }.Concat(modules);
            foreach (var moduleBase in moduleBases){
                if (application.Modules.All(_ => moduleBase.GetType() != _.GetType())){
                    application.Modules.Add(moduleBase);
                }
            }
            
            application.RegisterInMemoryObjectSpaceProvider();
        }
        static readonly Dictionary<string,int> ModulePorts=new Dictionary<string, int>(){
            {"AutoCommitModule",61457 },
            {"CloneMemberValueModule",61458 },
            {"CloneModelViewModule",61459 },
            {"GridListEditorModule",61460 },
            {"HideToolBarModule",61461 },
            {"ReactiveLoggerModule",61462 },
            {"ReactiveLoggerHubModule",61463 },
            {"MasterDetailModule",61464 },
            {"ModelMapperModule",61465 },
            {"ModelViewInheritanceModule",61466 },
            {"OneViewModule",61467 },
            {"ProgressBarViewItemModule",61468 },
            {"ReactiveModule",61469 },
            {"ReactiveWinModule",61470 },
            {"RefreshViewModule",61471 },
            {"SuppressConfirmationModule",61472 },
            {"ViewEditModeModule",61473 },
        };
        public static IObservable<IModelReactiveLogger> ConfigureModel<TModule>(this XafApplication application,bool transmitMessage=true) where TModule:ModuleBase{
            
            return application.WhenModelChanged()
                .Where(_ => application.Modules.Any(m => m is ReactiveLoggerModule))
                .Select(_ => {
                    var logger = application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger;
                    logger.TraceSources.Enabled = transmitMessage;
                    var port = ModulePorts[typeof(TModule).Name];
                    var modelLoggerPortsList = ((IModelServerPorts) logger).LoggerPorts;
                    var serverPort = modelLoggerPortsList.OfType<IModelLoggerServerPort>().First();
                    serverPort.Port = port;
                    serverPort.Enabled = logger.TraceSources.Enabled;
                    var clientRange = modelLoggerPortsList.OfType<IModelLoggerClientRange>().First();
                    modelLoggerPortsList.Enabled = logger.TraceSources.Enabled;
                    clientRange.StartPort = port;
                    clientRange.EndPort = port+1;
                    return logger;
                });

        }

        public static ModuleBase AddModule(this XafApplication application,ModuleBase moduleBase,string title=null,bool setup=true, params Type[] additionalExportedTypes){
            var applicationTitle = title??$"{moduleBase.GetType().Name}_Tests";
            application.Title = applicationTitle;
            moduleBase.AdditionalExportedTypes.AddRange(additionalExportedTypes);
            if (setup){
                application.SetupDefaults(moduleBase);
                return application.Modules.First(m => m.Name==moduleBase.Name);
            }

            return moduleBase;

        }

        public static T AddModule<T>(this XafApplication application, string title,params Type[] additionalExportedTypes) where T : ModuleBase, new(){
            return (T) application.AddModule(new T(),title,true, additionalExportedTypes);
        }

        public static T AddModule<T>(this XafApplication application,params Type[] additionalExportedTypes) where  T:ModuleBase, new(){
            return (T) application.AddModule(new T(),null,true, additionalExportedTypes);
        }

        public static TModule NewModule<TModule>(Platform platform,string title=null,params Type[] additionalExportedTypes) where  TModule:ModuleBase, new(){
            return platform.NewApplication<TModule>().AddModule<TModule>(title,additionalExportedTypes);
        }
        
        public static XafApplication NewApplication<TModule>(this Platform platform,bool transmitMessage=true) where TModule:ModuleBase{
            XafApplication application;
            
            if (platform == Platform.Web){
                application = new TestWebApplication(typeof(TModule),transmitMessage);
            }
            else if (platform == Platform.Win){
                application = new TestWinApplication(typeof(TModule),transmitMessage);
            }
            else{
                throw new NotSupportedException("if implemented make sure all tests pass with TestExplorer and live testing");
            }
            application.ConfigureModel<TModule>(transmitMessage).SubscribeReplay();
            application.MockEditorsFactory();

            if (application is WebApplication webApplication){
                var frameTemplateFactoryMock = new Mock<IFrameTemplateFactory>();
                var frameTemplateMock = new Mock<System.Web.UI.Control>(){CallBase = true}.As<IFrameTemplate>();
                frameTemplateMock.Setup(template => template.GetContainers()).Returns(new ActionContainerCollection());
                frameTemplateFactoryMock.Setup(factory => factory.CreateTemplate(It.IsAny<TemplateContext>())).Returns(
                    (TemplateContext context) => {
                        if (context == TemplateContext.NestedFrame){
                            return (IFrameTemplate) frameTemplateMock.As<ISupportActionsToolbarVisibility>().Object;
                        }

                        return frameTemplateMock.Object;
                    });
                webApplication.FrameTemplateFactory = frameTemplateFactoryMock.Object;
            }
            else {
                ((WinApplication) application).UseLightStyle = true;
            }
            return application;
        }

        static void MockEditorsFactory(this XafApplication application){
            var listEditorMock = ListEditorMock(application);

            var editorsFactoryMock = new Mock<IEditorsFactory>();
            application.EditorFactory =editorsFactoryMock.Object;
            application.MockListEditor( (view, xafApplication, collectionSource) => listEditorMock.Object);
            
            editorsFactoryMock.Setup(_ => _.CreateDetailViewEditor(It.IsAny<bool>(), It.IsAny<IModelViewItem>(),
                    It.IsAny<Type>(), It.IsAny<XafApplication>(), It.IsAny<IObjectSpace>()))
                .Returns((bool needProtectedContent, IModelViewItem modelViewItem, Type objectType, XafApplication app,
                    IObjectSpace objectSpace) => {
                    if (modelViewItem is IModelDashboardViewItem modelDashboardViewItem){
                        var detailViewEditor = new EditorsFactory().CreateDetailViewEditor(needProtectedContent,
                            modelDashboardViewItem, objectType, application, objectSpace);
                        return detailViewEditor;
                    }

                    return new Mock<ViewItem>(typeof(object), Guid.NewGuid().ToString()){CallBase = true}.Object;
                });
            
        }

        public static Mock<ListEditor> ListEditorMock(this XafApplication application){
            var listEditorMock = new Mock<ListEditor>{CallBase = true};
            listEditorMock.Setup(editor => editor.SupportsDataAccessMode(CollectionSourceDataAccessMode.Client)).Returns(true);
            listEditorMock.Setup(editor => editor.GetSelectedObjects()).Returns(new object[0]);
            listEditorMock.Protected().Setup<object>("CreateControlsCore")
                .Returns(application is WinApplication ? (object) new GridControl() : new ASPxGridView());
            return listEditorMock;
        }

        public static void MockListEditor(this XafApplication application,  Func<IModelListView,XafApplication,CollectionSourceBase,ListEditor> listEditor){
            var editorsFactoryMock = application.EditorFactory.GetMock();
            editorsFactoryMock.Setup(_ =>_.CreateListEditor(It.IsAny<IModelListView>(), It.IsAny<XafApplication>(),It.IsAny<CollectionSourceBase>()))
                .Returns((IModelListView modelListView, XafApplication app, CollectionSourceBase collectionSourceBase) =>listEditor(modelListView, application, collectionSourceBase));
        }

        public static ListEditor CreateListEditor(this XafApplication application, IModelListView modelListView,CollectionSourceBase collectionSourceBase){
            var listEditor = application is WinApplication
                ? (ListEditor) new Mock<GridListEditor>(modelListView){CallBase = true}.Object
                : new Mock<ASPxGridListEditor>(modelListView){CallBase = true}.Object;
            ((IComplexListEditor) listEditor).Setup(collectionSourceBase, application);
            return listEditor;
        }

        public static void MockDetailViewEditor(this XafApplication application,IModelPropertyEditor modelPropertyEditor,object controlInstance){
            modelPropertyEditor.PropertyEditorType = typeof(CustomPropertyEditor);
            application.EditorFactory.GetMock().Setup(factory => factory
                    .CreateDetailViewEditor(false, It.IsAny<IModelViewItem>(),
                        modelPropertyEditor.ModelMember.ModelClass.TypeInfo.Type, application, It.IsAny<IObjectSpace>()))
                .Returns((bool needProtectedContent, IModelMemberViewItem modelViewItem, Type objectType,
                    XafApplication xafApplication, IObjectSpace objectSpace) => {
                    if (modelViewItem == modelPropertyEditor){
                        return new CustomPropertyEditor(objectType, modelViewItem, controlInstance);
                    }

                    return new EditorsFactory().CreateDetailViewEditor(needProtectedContent, modelViewItem, objectType,application, objectSpace);
                });
        }

        public static void MockFrameTemplate(this XafApplication application){
            var frameTemplateMock = new Mock<IFrameTemplate>();
            frameTemplateMock.Setup(template => template.GetContainers()).Returns(() => new IActionContainer[0]);
            application.WhenCreateCustomTemplate()
                .Do(_ => _.e.Template = frameTemplateMock.Object)
                .Subscribe();
        }

        public static void RegisterInMemoryObjectSpaceProvider(this XafApplication application) {
            application.AddObjectSpaceProvider(new XPObjectSpaceProvider(new MemoryDataStoreProvider(),true));
        }


    }
}
