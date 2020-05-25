using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Templates.ActionContainers;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Web.SystemModule;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Web;
using DevExpress.XtraGrid;
using Fasterflect;
using JetBrains.Annotations;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Xpand.Extensions.Linq;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Action;
using Xpand.Extensions.XAF.Model;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.Extensions.XAF.Xpo;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Logger.Hub;
using Xpand.XAF.Modules.Reactive.Services;
using EditorsFactory = DevExpress.ExpressApp.Editors.EditorsFactory;

namespace Xpand.TestsLib{
    [PublicAPI]
    public static class Extensions{
        public static IObservable<Exception> WhenException(this TestTracing tracing){
            return Observable.FromEventPattern<EventHandler<CreateCustomTracerEventArgs>, CreateCustomTracerEventArgs>(
                    h => Tracing.CreateCustomTracer += h, h => Tracing.CreateCustomTracer -= h, ImmediateScheduler.Instance).FirstAsync()
                .SelectMany(_ => {
                    _.EventArgs.Tracer=tracing;
                    return tracing.Exceptions;
                });
        }

        public static void SetupSecurity(this XafApplication application){
            application.Modules.Add(new SecurityModule());
            application.Security = new SecurityStrategyComplex(typeof(PermissionPolicyUser),
                typeof(PermissionPolicyRole), new AuthenticationStandard(typeof(PermissionPolicyUser),
                    typeof(AuthenticationStandardLogonParameters)));
        }

        public static async Task<T> WithTimeOut<T>(this Task<T> source, TimeSpan? timeout = null){
            return await source.ToObservable().WithTimeOut(timeout);
        }

        public static async Task<T> WithTimeOut<T>(this IObservable<T> source, TimeSpan? timeout = null){
            timeout ??= TimeSpan.FromSeconds(5);
            return await source.Timeout(timeout.Value);
        }

        public static Mock<T> GetMock<T>(this T t) where T : class => Mock.Get(t);

        public static void OnSelectionChanged(this ListEditor editor){
	        editor.CallMethod(nameof(OnSelectionChanged));
        }

        public static void OnSelectionChanged(this ObjectView objectView){
	        objectView.CallMethod(nameof(OnSelectionChanged));
        }

        public static CompositeView NewView(this XafApplication application, IModelView modelView,Func<IObjectSpace,IList> selectedObjectsFactory){

	        application.WhenListViewCreating().Where(_ => _.e.ViewID == modelView.Id).Do(_ => {
		        var viewMock = new Mock<ListView>(() => new ListView((IModelListView) modelView, _.e.CollectionSource,_.application,_.e.IsRoot)){CallBase = true};
		        viewMock.As<ISelectionContext>().SetupGet(context => context.SelectedObjects)
			        .Returns(() => selectedObjectsFactory(viewMock.Object.ObjectSpace));
		        _.e.View = viewMock.Object;
	        }).FirstAsync().Subscribe();
	        return application.NewView(modelView);
        }

        public static void DoExecute(this ActionBase action,Func<IObjectSpace,IList> selectedObjectsFactory){
            var selectionContextMock = new Mock<ISelectionContext>();
            selectionContextMock.SetupGet(context => context.SelectedObjects).Returns(() => selectedObjectsFactory(action.View().ObjectSpace));
            action.SelectionContext = selectionContextMock.Object;
            action.Active[ ActionBase.RequireSingleObjectContext] = true;
            action.Active[ ActionBase.RequireMultipleObjectsContext] = true;
            action.DoTheExecute();
        }

        public static void MockCreateControls(this DashboardView view){
            foreach (var dashboardViewItem in view.GetItems<DashboardViewItem>()){
                dashboardViewItem.CreateControl();
            }

            view.CreateControls();
        }

        public static void SetupDefaults(this XafApplication application, params ModuleBase[] modules){
            application.RegisterDefaults(modules);
            application.Setup();
            if (!string.IsNullOrEmpty(application.ConnectionString)&&!application.ConnectionString.Contains(InMemoryDataStoreProvider.ConnectionString)){
                application.ObjectSpaceProvider.DeleteAllData();
            }
        }

        public static void RegisterDefaults(this XafApplication application, params ModuleBase[] modules){
            if (modules.Any() && application.Security is SecurityStrategyComplex){
                modules = modules.Add(new ModuleUpdaterModule()).ToArray();
            }

            application.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
            var moduleBases = new[]{
                new SystemModule(),
                application is WinApplication ? (ModuleBase) new SystemWindowsFormsModule() : new SystemAspNetModule()
            }.Concat(modules.Concat(new []{new ReactiveLoggerModule()}));

            if (((ITestApplication) application).TransmitMessage){
                if (Process.GetProcessesByName("Xpand.XAF.Modules.Reactive.Logger.Client.Win").Any()){
                    moduleBases = moduleBases.Add(new ReactiveLoggerHubModule());   
                }
            }

            foreach (var moduleBase in moduleBases){
                if (application.Modules.All(_ => moduleBase.GetType() != _.GetType())){
                    application.Modules.Add(moduleBase);
                }
            }

            application.AddObjectSpaceProvider();
        }

        public static readonly Dictionary<string, int> ModulePorts = new Dictionary<string, int>(){
            {"AutoCommitModule", 61457},
            {"CloneMemberValueModule", 61458},
            {"CloneModelViewModule", 61459},
            {"GridListEditorModule", 61460},
            {"HideToolBarModule", 61461},
            {"ReactiveLoggerModule", 61462},
            {"ReactiveLoggerHubModule", 61463},
            {"MasterDetailModule", 61464},
            {"ModelMapperModule", 61465},
            {"ModelViewInheritanceModule", 61466},
            {"OneViewModule", 61467},
            {"ProgressBarViewItemModule", 61468},
            {"ReactiveModule", 61469},
            {"ReactiveWinModule", 61470},
            {"RefreshViewModule", 61471},
            {"SuppressConfirmationModule", 61472},
            {"ViewEditModeModule", 61473},
            {"LookupCascadeModule", 61474},
            {"SequenceGeneratorModule", 61475},
            {"MicrosoftTodoModule", 61476},
            {"PositionInlistViewModule", 61478},
        };

        public static IObservable<IModelReactiveLogger> ConfigureModel<TModule>(this XafApplication application,
            bool transmitMessage = true) where TModule : ModuleBase{
            return application.WhenModelChanged()
                .Where(_ => application.Modules.Any(m => m is ReactiveLoggerModule))
                .Select(_ => {
                    var logger = application.Model.ToReactiveModule<IModelReactiveModuleLogger>()?.ReactiveLogger;
                    if (logger != null){
                        logger.TraceSources.Enabled = true;
                        var modelTraceSourcedModules = logger.TraceSources.Where(module => module.Id()!=typeof(TModule).Name);
                        var traceSourcedModule = logger.TraceSources.First(module => module.Id()==typeof(TModule).Name);
                        traceSourcedModule.Level=SourceLevels.Verbose;
                        
                        foreach (var modelTraceSourcedModule in modelTraceSourcedModules){
                            modelTraceSourcedModule.Level=SourceLevels.Off;    
                        }

                        var port = ModulePorts.Where(pair => pair.Key == typeof(TModule).Name)
                            .Select(pair => pair.Value).FirstOrDefault();
                        if (port > 0 && logger is IModelServerPorts modelServerPorts){
                            var modelLoggerPortsList = modelServerPorts.LoggerPorts;
                            var serverPort = modelLoggerPortsList.OfType<IModelLoggerServerPort>().First();
                            serverPort.Port = port;
                            serverPort.Enabled = transmitMessage;
                            var clientRange = modelLoggerPortsList.OfType<IModelLoggerClientRange>().First();
                            modelLoggerPortsList.Enabled = transmitMessage;
                            clientRange.StartPort = port;
                            clientRange.EndPort = port + 1;
                        }

                        return logger;
                    }

                    return null;
                })
                .WhenDefault();
        }

        public static ModuleBase AddModule(this XafApplication application, ModuleBase moduleBase, string title = null,
            bool setup = true, params Type[] additionalExportedTypes){
            var applicationTitle = title ?? $"{moduleBase.GetType().Name}_Tests";
            application.Title = applicationTitle;
            moduleBase.AdditionalExportedTypes.AddRange(additionalExportedTypes);
            if (setup){
                application.SetupDefaults(moduleBase);
                return application.Modules.FirstOrDefault(m => m.Name == moduleBase.Name);
            }

            return moduleBase;
        }

        public static T AddModule<T>(this XafApplication application, string title,
            params Type[] additionalExportedTypes) where T : ModuleBase, new(){
            return (T) application.AddModule(new T(), title, true, additionalExportedTypes);
        }

        public static T AddModule<T>(this XafApplication application, params Type[] additionalExportedTypes)
            where T : ModuleBase, new(){
            return (T) application.AddModule(new T(), null, true, additionalExportedTypes);
        }
    
        public static TModule NewModule<TModule>(Platform platform, string title = null,
            params Type[] additionalExportedTypes) where TModule : ModuleBase, new(){
            return platform.NewApplication<TModule>().AddModule<TModule>(title, additionalExportedTypes);
        }

        public static XafApplication NewApplication<TModule>(this Platform platform, bool transmitMessage = true,bool handleExceptions=true,bool usePersistentStorage=false)
            where TModule : ModuleBase{
            XafApplication application;

            if (platform == Platform.Web){
                application = new TestWebApplication(typeof(TModule), transmitMessage);
            }
            else if (platform == Platform.Win){
                application = new TestWinApplication(typeof(TModule), transmitMessage,handleExceptions);
            }
            else{
                throw new NotSupportedException(
                    "if implemented make sure all tests pass with TestExplorer and live testing");
            }

            application.Title = TestContext.CurrentContext.Test.FullName;
            application.ConnectionString =usePersistentStorage?
                @$"Integrated Security=SSPI;Pooling=false;Data Source=(localdb)\mssqllocaldb;Initial Catalog={typeof(TModule).Name}":InMemoryDataStoreProvider.ConnectionString;
            application.DatabaseUpdateMode=DatabaseUpdateMode.UpdateDatabaseAlways;
            application.CheckCompatibilityType=CheckCompatibilityType.DatabaseSchema;
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
            else{
                ((WinApplication) application).UseLightStyle = true;
            }

            return application;
        }

        static void MockEditorsFactory(this XafApplication application){
            var editorsFactoryMock = new Mock<IEditorsFactory>();
            application.EditorFactory = editorsFactoryMock.Object;
            application.MockListEditor();

            editorsFactoryMock.Setup(_ => _.CreateDetailViewEditor(It.IsAny<bool>(), It.IsAny<IModelViewItem>(), It.IsAny<Type>(), It.IsAny<XafApplication>(), It.IsAny<IObjectSpace>()))
                .Returns((bool needProtectedContent, IModelViewItem modelViewItem, Type objectType, XafApplication app, IObjectSpace objectSpace) 
                    => new EditorsFactory().CreateDetailViewEditor(needProtectedContent, modelViewItem, objectType, application, objectSpace));
            editorsFactoryMock.Setup(_ => _.CreatePropertyEditorByType(It.IsAny<Type>(),It.IsAny<IModelMemberViewItem>(),It.IsAny<Type>(),It.IsAny<XafApplication>(),It.IsAny<IObjectSpace>()))
                .Returns((Type editorType, IModelMemberViewItem modelViewItem, Type objectType, XafApplication xafApplication, IObjectSpace objectSpace)
                    =>new EditorsFactory().CreatePropertyEditorByType(editorType, modelViewItem, objectType, xafApplication, objectSpace));
        }


        public static Mock<ListEditor> ListEditorMock(this XafApplication application, IModelListView listView){
	        return application.ListEditorMock<ListEditor>(listView);
        }

        public static Mock<TEditor> ListEditorMock<TEditor>(this XafApplication application ,IModelListView listView) where TEditor :  ListEditor{
	        var listEditorMock = new Mock<TEditor>(listView){CallBase = true};
            listEditorMock.Setup(editor => editor.SupportsDataAccessMode(CollectionSourceDataAccessMode.Client)).Returns(true);
            listEditorMock.Setup(editor => editor.GetSelectedObjects()).Returns(new object[0]);
            listEditorMock.Protected().Setup<object>("CreateControlsCore")
                .Returns(application is WinApplication ? (object) new GridControl() : new ASPxGridView());
            return listEditorMock;
        }

        public static void MockPlatformListEditor(this XafApplication application){
           application.MockListEditor((view, xafApplication, collectionSource) => application is WinApplication
	           ? (ListEditor) new GridListEditor(view) : new ASPxGridListEditor(view));
        }

        public static void MockListEditor(this XafApplication application, Func<IModelListView, XafApplication, CollectionSourceBase, ListEditor> listEditor = null){
	        listEditor ??= ((view, xafApplication, arg3) => application.ListEditorMock(view).Object);
           var editorsFactoryMock = application.EditorFactory.GetMock();
           editorsFactoryMock.Setup(_ => _.CreateListEditor(It.IsAny<IModelListView>(), It.IsAny<XafApplication>(), It.IsAny<CollectionSourceBase>()))
                .Returns((IModelListView modelListView, XafApplication app, CollectionSourceBase collectionSourceBase) =>
                        listEditor(modelListView, application, collectionSourceBase));
        }

        public static ListEditor CreateListEditor(this XafApplication application, IModelListView modelListView, CollectionSourceBase collectionSourceBase){
            var listEditor = application is WinApplication ? (ListEditor) new Mock<GridListEditor>(modelListView){CallBase = true}.Object
                : new Mock<ASPxGridListEditor>(modelListView){CallBase = true}.Object;
            ((IComplexListEditor) listEditor).Setup(collectionSourceBase, application);
            return listEditor;
        }

        public static void MockDetailViewEditor(this XafApplication application,
            IModelPropertyEditor modelPropertyEditor, object controlInstance){
            modelPropertyEditor.PropertyEditorType = typeof(CustomPropertyEditor);
            application.EditorFactory.GetMock().Setup(factory => factory
                    .CreateDetailViewEditor(false, It.IsAny<IModelViewItem>(),
                        modelPropertyEditor.ModelMember.ModelClass.TypeInfo.Type, application,
                        It.IsAny<IObjectSpace>()))
                .Returns((bool needProtectedContent, IModelMemberViewItem modelViewItem, Type objectType,
                    XafApplication xafApplication, IObjectSpace objectSpace) => {
                    if (modelViewItem == modelPropertyEditor){
                        return new CustomPropertyEditor(objectType, modelViewItem, controlInstance);
                    }

                    return new EditorsFactory().CreateDetailViewEditor(needProtectedContent, modelViewItem, objectType,
                        application, objectSpace);
                });
        }

        public static void MockFrameTemplate(this XafApplication application){
            var frameTemplateMock = new Mock<IFrameTemplate>();
            frameTemplateMock.Setup(template => template.GetContainers()).Returns(() => new IActionContainer[0]);
            application.WhenCreateCustomTemplate()
                .Do(_ => _.e.Template = frameTemplateMock.Object)
                .Subscribe();
        }

    }

    public class ModuleUpdaterModule : ModuleBase{
        public override void Setup(XafApplication application){
            base.Setup(application);
            application.LoggingOn += ApplicationOnLoggingOn;
        }

        private void ApplicationOnLoggingOn(object sender, LogonEventArgs e){
            ((AuthenticationStandardLogonParameters) e.LogonParameters).UserName = "User";
        }


        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB){
            return base.GetModuleUpdaters(objectSpace, versionFromDB)
                .Add(new DefaultUserModuleUpdater(objectSpace, versionFromDB));
        }
    }

    public class DefaultUserModuleUpdater : ModuleUpdater{
        public DefaultUserModuleUpdater(IObjectSpace objectSpace, Version currentDBVersion) : base(objectSpace,
            currentDBVersion){
        }

        private PermissionPolicyRole CreateDefaultRole(){
            var defaultRole = ObjectSpace.FindObject<PermissionPolicyRole>(new BinaryOperator("Name", "Default"));
            if (defaultRole == null){
                defaultRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                defaultRole.Name = "Default";

                defaultRole.AddObjectPermission<PermissionPolicyUser>(SecurityOperations.Read,
                    "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
                defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails",
                    SecurityPermissionState.Allow);
                defaultRole.AddMemberPermission<PermissionPolicyUser>(SecurityOperations.Write,
                    "ChangePasswordOnFirstLogon", "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
                defaultRole.AddMemberPermission<PermissionPolicyUser>(SecurityOperations.Write, "StoredPassword",
                    "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read,
                    SecurityPermissionState.Deny);
                defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.ReadWriteAccess,
                    SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess,
                    SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create,
                    SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create,
                    SecurityPermissionState.Allow);
            }

            return defaultRole;
        }

        public override void UpdateDatabaseAfterUpdateSchema(){
            base.UpdateDatabaseAfterUpdateSchema();

            var sampleUser = ObjectSpace.FindObject<PermissionPolicyUser>(new BinaryOperator("UserName", "User"));
            if (sampleUser == null){
                sampleUser = ObjectSpace.CreateObject<PermissionPolicyUser>();
                sampleUser.UserName = "User";
                sampleUser.SetPassword("");
            }

            var defaultRole = CreateDefaultRole();
            sampleUser.Roles.Add(defaultRole);

            var userAdmin = ObjectSpace.FindObject<PermissionPolicyUser>(new BinaryOperator("UserName", "Admin"));
            if (userAdmin == null){
                userAdmin = ObjectSpace.CreateObject<PermissionPolicyUser>();
                userAdmin.UserName = "Admin";
                userAdmin.SetPassword("");
            }

            var adminRole = ObjectSpace.FindObject<PermissionPolicyRole>(new BinaryOperator("Name", "Administrators"));
            if (adminRole == null){
                adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                adminRole.Name = "Administrators";
            }

            adminRole.IsAdministrative = true;
            userAdmin.Roles.Add(adminRole);
            ObjectSpace.CommitChanges();
        }
    }
}