using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Templates.ActionContainers;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;
using Fasterflect;

using Moq;
using Moq.Protected;
using NUnit.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Logger.Hub;
using Xpand.XAF.Modules.Reactive.Services;
using EditorsFactory = DevExpress.ExpressApp.Editors.EditorsFactory;

namespace Xpand.TestsLib.Common{
    
    public static class Extensions{
        public static IObservable<Exception> WhenException(this TestTracing tracing) 
            => Observable.FromEventPattern<EventHandler<CreateCustomTracerEventArgs>, CreateCustomTracerEventArgs>(
                    h => Tracing.CreateCustomTracer += h, h => Tracing.CreateCustomTracer -= h, ImmediateScheduler.Instance).FirstAsync()
                .SelectMany(_ => {
                    _.EventArgs.Tracer=tracing;
                    return tracing.Exceptions;
                });

        public static Guid SetupSecurity(this XafApplication application, Guid userId,Type userType=null,Type roleType=null,bool notAdmin=false) {
            userType ??= typeof(PermissionPolicyUser);
            roleType ??= typeof(PermissionPolicyRole);
            application.Modules.Add(new SecurityModule());
            var testApplicationModule = application.Modules.FindModule<TestApplicationModule>();
            if (testApplicationModule == null){
                testApplicationModule=new TestApplicationModule();
                application.Modules.Add(testApplicationModule);
            }
            testApplicationModule.UserId = userId;
            testApplicationModule.NotAdmin = notAdmin;
            application.Security = new SecurityStrategyComplex(userType, roleType, new AuthenticationStandard(userType, typeof(AuthenticationStandardLogonParameters)));
            return userId;
        }

        public static Guid SetupSecurity(this XafApplication application,bool fixedUserId=false,Type userType=null,Type roleType=null) 
            => application.SetupSecurity(fixedUserId?Guid.Parse("5c50f5c6-e697-4e9e-ac1b-969eac1237f3") :Guid.NewGuid() ,userType,roleType);

        public static async Task<T> WithTimeOut<T>(this Task<T> source, TimeSpan? timeout = null) => await source.ToObservable().WithTimeOut(timeout);

        public static async Task<T> WithTimeOut<T>(this IObservable<T> source, TimeSpan? timeout = null){
            timeout ??= TimeSpan.FromSeconds(5);
            return await source.Timeout(timeout.Value);
        }

        public static void OnSelectionChanged(this ListEditor editor) => editor.CallMethod(nameof(OnSelectionChanged));

        public static void OnSelectionChanged(this ObjectView objectView) => objectView.CallMethod(nameof(OnSelectionChanged));

        public static CompositeView NewView(this XafApplication application, IModelView modelView,Func<IObjectSpace,IList> selectedObjectsFactory,IObjectSpace objectSpace=null){
	        application.WhenListViewCreating().Where(_ => _.e.ViewID == modelView.Id).Do(_ => {
		        var viewMock = new Mock<ListView>(() => new ListView((IModelListView) modelView, _.e.CollectionSource,_.application,_.e.IsRoot)){CallBase = true};
		        viewMock.As<ISelectionContext>().SetupGet(context => context.SelectedObjects)
			        .Returns(() => selectedObjectsFactory(viewMock.Object.ObjectSpace));
		        _.e.View = viewMock.Object;
	        }).FirstAsync().Subscribe();
	        return application.NewView(modelView,objectSpace);
        }

        public static void DoExecute(this ActionBase action,Func<IObjectSpace,IList> selectedObjectsFactory,bool force=false){
            var selectionContextMock = new Mock<ISelectionContext>();
            selectionContextMock.SetupGet(context => context.SelectedObjects).Returns(() => selectedObjectsFactory(action.View().ObjectSpace));
            selectionContextMock.SetupGet(context => context.CurrentObject).Returns(() => selectedObjectsFactory(action.View().ObjectSpace).Cast<object>().First());
            action.SelectionContext = selectionContextMock.Object;
            action.Active[ ActionBase.RequireSingleObjectContext] = true;
            action.Active[ ActionBase.RequireMultipleObjectsContext] = true;
            action.DoTheExecute(force);
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
                if (!modules.OfType<TestApplicationModule>().Any()){
                    modules=modules.AddToArray(new TestApplicationModule());
                }
            }

            application.AlwaysUpdateOnDatabaseVersionMismatch().FirstAsync().Subscribe();
            object instance;
            var platform = application.GetPlatform();
            if (platform == Platform.Win)
                instance = (ModuleBase) AppDomain.CurrentDomain
                    .GetAssemblyType("DevExpress.ExpressApp.Win.SystemModule.SystemWindowsFormsModule")
                    .CreateInstance();
            else if (platform==Platform.Web) {
                instance = AppDomain.CurrentDomain
                    .GetAssemblyType("DevExpress.ExpressApp.Web.SystemModule.SystemAspNetModule")
                    .CreateInstance();
            }
            else {
                instance = AppDomain.CurrentDomain
                    .GetAssemblyType("DevExpress.ExpressApp.Blazor.SystemModule.SystemBlazorModule")
                    .CreateInstance();
            }
            var moduleBases = new[]{
                new SystemModule(),
                (ModuleBase) instance
            }
            .Concat(modules)
            // .Concat(new []{new ReactiveLoggerModule()})
            .ToList();

            if (((ITestApplication) application).TransmitMessage){
                if (Process.GetProcessesByName("Xpand.XAF.Modules.Reactive.Logger.Client.Win").Any()){
                    moduleBases.Add(new ReactiveLoggerHubModule());   
                }
            }

            foreach (var moduleBase in moduleBases){
                if (application.Modules.All(_ => moduleBase.GetType() != _.GetType())){
                    application.Modules.AddRange(new []{moduleBase});
                }
            }

            application.AddObjectSpaceProvider();
        }

        public static readonly Dictionary<string, int> ModulePorts = new(){
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
            {"PositionInListViewModule", 61478},
            {"ViewWizardModule", 61482},
            {"ViewItemValueModule", 61479},
            {"MicrosoftModule", 61480},
            {"MicrosoftCalendarModule", 61481},
            {"GoogleModule", 61483},
            {"GoogleTasksModule", 61484},
            {"GoogleCalendarModule", 61485},
            {"DocumentStyleManagerModule", 61486},
            {"JobSchedulerModule", 61487},
            {"RestModule", 61488},
            {"WindowsModule", 61489},
            {"JobSchedulerNotificationModule", 61490},
            {"RazorViewModule", 61491},
            {"EmailModule", 61492},
            {"TenantManagerModule", 61493},
            {"SpeechManagerModule", 61494},
        };

        public static TestObserver<T> StartTest<T>(this XafApplication application,IObservable<T> test,int delay=200) {
            var testObserver = test
                .Timeout(TimeSpan.FromSeconds(10))
                .AsyncFinally(async () => await Task.Delay(delay).ToObservable().Do(_ => application.Exit()).ToTask())
                .Test();
            application.CallMethod("Start");
            return testObserver;
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        public static IObservable<IModelReactiveLogger> ConfigureModel<TModule>(this XafApplication application,
            bool transmitMessage = true) where TModule : ModuleBase{
            return application.WhenModelChanged().FirstAsync()
                .Where(_ => application.Modules.Any(m => m is ReactiveLoggerModule))
                .Select(_ => {
                    var logger = application.Model.ToReactiveModule<IModelReactiveModuleLogger>()?.ReactiveLogger;
                    if (logger != null){
                        logger.TraceSources.Enabled = true;
                        logger.TraceSources.PersistStrategy=ObservableTraceStrategy.All;
                        var modelTraceSourcedModules = logger.TraceSources.Where(module => module.Id()!=typeof(TModule).Name);
                        var traceSourcedModule = logger.TraceSources.FirstOrDefault(module => module.Id()==typeof(TModule).Name);
                        if (traceSourcedModule != null) {
                            traceSourcedModule.Level = SourceLevels.Verbose;
                        }
                        else {
                            return logger;
                        }

                        foreach (var modelTraceSourcedModule in modelTraceSourcedModules){
                            modelTraceSourcedModule.Level=SourceLevels.Off;    
                        }

                        var port = ModulePorts.Where(pair => pair.Key == typeof(TModule).Name)
                            .Select(pair => pair.Value).FirstOrDefault();
                        if (port > 0 && logger is IModelReactiveLoggerHub modelServerPorts){
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
            var applicationTitle = title ?? application.Title;
            application.Title = applicationTitle;
            moduleBase.AdditionalExportedTypes.AddRange(additionalExportedTypes);
            if (setup){
                application.SetupDefaults(moduleBase);
                return application.Modules.FirstOrDefault(m => m.Name == moduleBase.Name);
            }

            return moduleBase;
        }

        public static T AddModule<T>(this XafApplication application, string title,
            params Type[] additionalExportedTypes) where T : ModuleBase, new() 
            => (T) application.AddModule(new T(), title, true, additionalExportedTypes);
        public static T AddModule<T>(this XafApplication application, string title,bool setup,
            params Type[] additionalExportedTypes) where T : ModuleBase, new() 
            => (T) application.AddModule(new T(), title, setup, additionalExportedTypes);

        public static T AddModule<T>(this XafApplication application, params Type[] additionalExportedTypes) where T : ModuleBase, new() 
            => (T) application.AddModule(new T(), null, true, additionalExportedTypes);

        public static TModule NewModule<TModule>(this Platform platform, string title = null,
            params Type[] additionalExportedTypes) where TModule : ModuleBase, new() 
            => platform.NewApplication<TModule>().AddModule<TModule>(title, additionalExportedTypes);

        public static Type ApplicationType { get; set; }
        public static XafApplication NewApplication<TModule>(this Platform platform, bool transmitMessage = true,bool handleExceptions=true,bool usePersistentStorage=false)
            where TModule : ModuleBase 
	        => platform.NewXafApplication<TModule>(transmitMessage, handleExceptions).Configure<TModule>(platform, transmitMessage, usePersistentStorage);

        public static XafApplication Configure<TModule>(this XafApplication application,Platform platform, bool transmitMessage = true, bool usePersistentStorage=false) where TModule : ModuleBase {
	        application.ConnectionString = usePersistentStorage
		        ? @$"Integrated Security=SSPI;Pooling=false;Data Source=(localdb)\mssqllocaldb;Initial Catalog={typeof(TModule).Name}"
		        : InMemoryDataStoreProvider.ConnectionString;
	        application.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
	        application.CheckCompatibilityType = CheckCompatibilityType.DatabaseSchema;
	        application.ConfigureModel<TModule>(transmitMessage).SubscribeReplay();
	        application.MockEditorsFactory();

	        if (platform == Platform.Web) {
		        var frameTemplateFactoryMock = new Mock<IFrameTemplateFactory>();
		        var frameTemplateMock = new Mock<IFrameTemplate>() { CallBase = true };
		        frameTemplateMock.Setup(template => template.GetContainers()).Returns(new ActionContainerCollection());
		        frameTemplateFactoryMock.Setup(factory => factory.CreateTemplate(It.IsAny<TemplateContext>())).Returns(
			        (TemplateContext context) => context == TemplateContext.NestedFrame
				        ? (IFrameTemplate)frameTemplateMock.As<ISupportActionsToolbarVisibility>().Object
				        : frameTemplateMock.Object);
		        application.SetPropertyValue("FrameTemplateFactory", frameTemplateFactoryMock.Object);
	        }
	        else if (platform == Platform.Win) {
		        application.SetPropertyValue("UseLightStyle", true);
	        }

	        return application;
        }

        public static XafApplication NewXafApplication<TModule>(this Platform platform, bool transmitMessage=true, bool handleExceptions=true)
	        where TModule : ModuleBase {
	        Tracing.Initialize(AppDomain.CurrentDomain.ApplicationPath(), TraceLevel.Verbose.ToString());
	        XafApplication application;
	        ApplicationModulesManager.UseStaticCache = false;
	        string applicationTypeName;
	        if (platform == Platform.Web) {
		        applicationTypeName = "Xpand.TestsLib.TestWebApplication";
		        application = (XafApplication)AppDomain.CurrentDomain.CreateTypeInstance(applicationTypeName, typeof(TModule), transmitMessage);
	        }
	        else if (platform == Platform.Win) {
		        applicationTypeName = "Xpand.TestsLib.TestWinApplication";
		        application = (XafApplication)AppDomain.CurrentDomain.CreateTypeInstance(applicationTypeName, typeof(TModule),
			        transmitMessage, handleExceptions);
		        var userModelPath = $"{AppDomain.CurrentDomain.ApplicationPath()}Model.User.xafml";
		        if (File.Exists(userModelPath)) {
			        File.Delete(userModelPath);
		        }
	        }
	        else if (platform == Platform.Blazor) {
		        application =
			        (XafApplication)ApplicationType.CreateInstance(typeof(TModule), transmitMessage, handleExceptions);
		        application.WhenWindowCreated()
			        .Do(window => {
				        var windowTemplate = (IWindowTemplate)AppDomain.CurrentDomain
					        .CreateTypeInstance("DevExpress.ExpressApp.Blazor.Templates.ApplicationWindowTemplate");
				        window.SetTemplate(windowTemplate);
			        })
			        .Subscribe(application);
	        }
	        else {
		        throw new NotSupportedException(
			        "if implemented make sure all tests pass with TestExplorer and live testing");
	        }
	        application.Title = TestContext.CurrentContext.Test.FullName;
	        return application;
        }

        static void MockEditorsFactory(this XafApplication application){
            var editorsFactoryMock = new Mock<IEditorsFactory>();
            application.EditorFactory = editorsFactoryMock.Object;
            application.MockListEditor();

            editorsFactoryMock.Setup(_ => _.CreateDetailViewEditor(It.IsAny<bool>(), It.IsAny<IModelViewItem>(), It.IsAny<Type>(), It.IsAny<XafApplication>(), It.IsAny<IObjectSpace>()))
                .Returns((bool needProtectedContent, IModelViewItem modelViewItem, Type objectType, XafApplication _, IObjectSpace objectSpace) 
                    => new EditorsFactory().CreateDetailViewEditor(needProtectedContent, modelViewItem, objectType, application, objectSpace));
            
            editorsFactoryMock.Setup(_ => _.CreateDetailViewEditor(It.IsAny<bool>(), It.IsAny<IModelMemberViewItem>(), It.IsAny<Type>(), It.IsAny<XafApplication>(), It.IsAny<IObjectSpace>()))
                .Returns((bool needProtectedContent, IModelMemberViewItem modelViewItem, Type objectType, XafApplication _, IObjectSpace objectSpace) 
                    => new EditorsFactory().CreateDetailViewEditor(needProtectedContent, modelViewItem, objectType, application, objectSpace));
            editorsFactoryMock.Setup(_ => _.CreatePropertyEditorByType(It.IsAny<Type>(),It.IsAny<IModelMemberViewItem>(),It.IsAny<Type>(),It.IsAny<XafApplication>(),It.IsAny<IObjectSpace>()))
                .Returns((Type editorType, IModelMemberViewItem modelViewItem, Type objectType, XafApplication xafApplication, IObjectSpace objectSpace)
                    =>new EditorsFactory().CreatePropertyEditorByType(editorType, modelViewItem, objectType, xafApplication, objectSpace));
        }

        

        public static Mock<ListEditor> ListEditorMock(this XafApplication application, IModelListView listView) => application.ListEditorMock<ListEditor>(listView);

        public static Mock<TEditor> ListEditorMock<TEditor>(this XafApplication application ,IModelListView listView) where TEditor :  ListEditor{
	        var listEditorMock = new Mock<TEditor>(listView){CallBase = true};
            listEditorMock.Setup(editor => editor.SupportsDataAccessMode(CollectionSourceDataAccessMode.Client)).Returns(true);
            listEditorMock.Setup(editor => editor.GetSelectedObjects()).Returns(Array.Empty<object>());
            listEditorMock.Setup(editor => editor.SelectionType).Returns(() => application.GetPlatform()==Platform.Win?SelectionType.Full : SelectionType.MultipleSelection);
            var platform = application.GetPlatform();
            if (platform!=Platform.Blazor) {
                listEditorMock.Protected().Setup<object>("CreateControlsCore")
                    .Returns(platform==Platform.Win ? AppDomain.CurrentDomain
                        .CreateTypeInstance("DevExpress.XtraGrid.GridControl") : AppDomain.CurrentDomain.CreateTypeInstance("DevExpress.Web.ASPxGridView"));
            }
            if (typeof(TEditor) == typeof(ListEditor)){
                application.WhenViewOnFrame(typeof(object), ViewType.ListView)
                    .Where(frame => frame.View.AsListView().Editor==listEditorMock.Object)
                    .Do(frame => listEditorMock.SetupSet(editor => editor.FocusedObject = It.IsAny<object>())
                        .Callback(() => frame.View.AsListView().OnSelectionChanged()))
                    .TakeUntilDisposed(application)
                    .Subscribe();
            }
            return listEditorMock;
        }

        public static void MockPlatformListEditor(this XafApplication application) 
            => application.MockListEditor((view, _, _) => (ListEditor) (application.GetPlatform()==Platform.Win
                ? (ListEditor) Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Win.Editors.GridListEditor"),view)
                : Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Web.Editors.ASPx.ASPxGridListEditor"),view)));

        public static string MemberExpressionCaption<TObject>(this Expression<Func<TObject, object>> memberName){
            var name = memberName.Body is UnaryExpression unaryExpression
                ? ((MemberExpression) unaryExpression.Operand).Member.Name
                : ((MemberExpression) memberName.Body).Member.Name;
            var displayNameAttribute =
                typeof(TObject).Property(name).Attribute<XafDisplayNameAttribute>() ??
                (Attribute) typeof(TObject).Property(name).Attribute<DisplayNameAttribute>() ?? typeof(TObject)
                    .Property(name).Attribute<System.ComponentModel.DisplayNameAttribute>();

            return (string) (displayNameAttribute?.GetPropertyValue("DisplayName")??name);
        }

        internal static string MemberExpressionCaption<TObject,TMemberValue>(this Expression<Func<TObject, TMemberValue>> memberName) 
            => memberName.Body is UnaryExpression unaryExpression
                ? ((MemberExpression) unaryExpression.Operand).Member.Name
                : ((MemberExpression) memberName.Body).Member.Name;

        public static void MockListEditor(this XafApplication application, Func<IModelListView, XafApplication, CollectionSourceBase, ListEditor> listEditor = null){
	        listEditor ??= ((view, _, _) => application.ListEditorMock(view).Object);
           var editorsFactoryMock = application.EditorFactory.GetMock();
           editorsFactoryMock.Setup(_ => _.CreateListEditor(It.IsAny<IModelListView>(), It.IsAny<XafApplication>(), It.IsAny<CollectionSourceBase>()))
                .Returns((IModelListView modelListView, XafApplication _, CollectionSourceBase collectionSourceBase) =>
                        listEditor(modelListView, application, collectionSourceBase));
        }

        public static ListEditor CreateListEditor(this XafApplication application, IModelListView modelListView, CollectionSourceBase collectionSourceBase) {
            var instance = (Mock) typeof(Mock<>).CreateInstance(
                AppDomain.CurrentDomain.GetAssemblyType(application.GetPlatform() == Platform.Win
                    ? "DevExpress.ExpressApp.Win.Editors.GridListEditor"
                    : "DevExpress.ExpressApp.Web.Editors.ASPx.ASPxGridListEditor"), modelListView);
            instance.CallBase = true;
            var listEditor = instance.Object;
            ((IComplexListEditor) listEditor).Setup(collectionSourceBase, application);
            return (ListEditor) listEditor;
        }

        public static void OnCurrentObjectChanged(this ObjectView objectView) => objectView.CallMethod("OnCurrentObjectChanged");

        public static void MockDetailViewEditor(this XafApplication application,
            IModelPropertyEditor modelPropertyEditor, object controlInstance){
            modelPropertyEditor.PropertyEditorType = typeof(CustomPropertyEditor);
            application.EditorFactory.GetMock().Setup(factory => factory
                    .CreateDetailViewEditor(false, It.IsAny<IModelViewItem>(),
                        modelPropertyEditor.ModelMember.ModelClass.TypeInfo.Type, application,
                        It.IsAny<IObjectSpace>()))
                .Returns((bool needProtectedContent, IModelMemberViewItem modelViewItem, Type objectType,
                    XafApplication _, IObjectSpace objectSpace) => {
                    if (modelViewItem == modelPropertyEditor){
                        return new CustomPropertyEditor(objectType, modelViewItem, controlInstance);
                    }

                    return new EditorsFactory().CreateDetailViewEditor(needProtectedContent, modelViewItem, objectType,
                        application, objectSpace);
                });
        }


        public static void MockFrameTemplate(this XafApplication application){
            var frameTemplateMock = new Mock<IWindowTemplate>();
            frameTemplateMock.Setup(template => template.GetContainers()).Returns(Array.Empty<IActionContainer>);
            application.WhenCreateCustomTemplate()
                .Do(_ => _.e.Template = frameTemplateMock.Object)
                .Subscribe();
        }

        public static IObservable<Unit> ClientBroadcast(this ITestApplication application) 
            => Process.GetProcessesByName("Xpand.XAF.Modules.Reactive.Logger.Client.Win").Any()
                ? TraceEventHub.Trace.FirstAsync(_ => _.Source == application.SUTModule.Name).ToUnit()
                    .SubscribeReplay()
                : Unit.Default.ReturnObservable();

        
        public static IObservable<Unit> ClientConnect(this ITestApplication application) 
            => Process.GetProcessesByName("Xpand.XAF.Modules.Reactive.Logger.Client.Win").Any()
                ? TraceEventHub.Connecting.FirstAsync().SubscribeReplay()
                : Unit.Default.ReturnObservable();
    }

    public class TestApplicationModule : ModuleBase{
        public override void Setup(XafApplication application){
            base.Setup(application);
            application.LoggingOn += ApplicationOnLoggingOn;
            
        }

        
        public override bool IsExportedType(Type type) 
            => (type.Namespace != typeof(Order).Namespace || !SkipCommonLibBOExport) && base.IsExportedType(type);

        public static bool SkipCommonLibBOExport { get; set; }

        private void ApplicationOnLoggingOn(object sender, LogonEventArgs e) => ((AuthenticationStandardLogonParameters) e.LogonParameters).UserName = NotAdmin?"User":"Admin";

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) 
            => new[]{new DefaultUserModuleUpdater(objectSpace, versionFromDB,UserId,!NotAdmin)};

        public Guid UserId{ get; set; }
        public bool NotAdmin { get; set; }
    }

    public interface ITestApplication{
        bool TransmitMessage{ get; }
        IObservable<Unit> TraceClientBroadcast{ get; set; }
        IObservable<Unit> TraceClientConnected{ get; set; }
        Type SUTModule{ get; }
    }

}