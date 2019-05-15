using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Templates.ActionContainers;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;
using Moq;
using Moq.Protected;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Services;
using Control = System.Windows.Forms.Control;

namespace Tests.Artifacts {
    static class Extensions {

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
            application.Modules.AddRange(modules);
            application.RegisterInMemoryObjectSpaceProvider();
        }

        public static XafApplication NewApplication(this Platform platform){
            var application = platform == Platform.Web ? (XafApplication) new TestWebApplication() : new TestWinApplication();

            var listEditorMock = new Mock<ListEditor>{CallBase = true};
            
            listEditorMock.Setup(editor => editor.SupportsDataAccessMode(CollectionSourceDataAccessMode.Client)).Returns(true);
            listEditorMock.Setup(editor => editor.GetSelectedObjects()).Returns(new object[0]);
            listEditorMock.Protected().Setup<object>("CreateControlsCore").Returns(new Control());
            
            
            var editorsFactoryMock = new Mock<IEditorsFactory>();
            var editorsFactory = new EditorsFactory();
            editorsFactoryMock.Setup(_ => _.CreateListEditor(It.IsAny<IModelListView>(),
                It.IsAny<XafApplication>(), It.IsAny<CollectionSourceBase>())).Returns(() => listEditorMock.Object);

            editorsFactoryMock.Setup(_ => _.CreateDetailViewEditor(It.IsAny<bool>(), It.IsAny<IModelViewItem>(),It.IsAny<Type>(), It.IsAny<XafApplication>(), It.IsAny<IObjectSpace>()))
                .Returns((bool needProtectedContent,IModelViewItem modelViewItem, Type objectType, XafApplication app, IObjectSpace objectSpace) => {
                    if (modelViewItem is IModelDashboardViewItem modelDashboardViewItem){
                        var detailViewEditor = editorsFactory.CreateDetailViewEditor(needProtectedContent, modelDashboardViewItem, objectType, application, objectSpace);
                        return detailViewEditor;
                    }
                    return new Mock<ViewItem>(typeof(object), Guid.NewGuid().ToString()){CallBase = true}.Object;
                });
            
            application.EditorFactory = editorsFactoryMock.Object;

            if (application is WebApplication webApplication){
                var frameTemplateFactoryMock = new Mock<IFrameTemplateFactory>();
                var frameTemplateMock = new Mock<System.Web.UI.Control>(){CallBase = true}.As<IFrameTemplate>();
                frameTemplateMock.Setup(template => template.GetContainers()).Returns(new ActionContainerCollection());
                frameTemplateFactoryMock.Setup(factory => factory.CreateTemplate(It.IsAny<TemplateContext>())).Returns(frameTemplateMock.Object);
                webApplication.FrameTemplateFactory = frameTemplateFactoryMock.Object;
            }
            else{
                ((WinApplication) application).UseLightStyle = true;
            }
            return application;
        }

        public static void MockFrameTemplate(this XafApplication application){
            var frameTemplateMock = new Mock<IFrameTemplate>();
            frameTemplateMock.Setup(template => template.GetContainers()).Returns(() => new IActionContainer[0]);
            application.WhenCreateCustomTemplate()
                .Do(_ => _.e.Template = frameTemplateMock.Object)
                .Subscribe();
        }

        public static void RegisterInMemoryObjectSpaceProvider(this XafApplication application) {
            application.AddObjectSpaceProvider(new XPObjectSpaceProvider(new MemoryDataStoreProvider()));
        }


    }
}
