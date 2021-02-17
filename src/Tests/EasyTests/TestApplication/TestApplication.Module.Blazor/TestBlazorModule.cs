using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using TestApplication.Module.Blazor.JobScheduler;
using Xpand.XAF.Modules.Blazor.Editors;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace TestApplication.Module.Blazor{
    [DefaultClassOptions]
    public class MyClass1:BaseObject {
        public MyClass1(Session session) : base(session) { }
        string _url;
        public override void AfterConstruction() {
            base.AfterConstruction();
            Url = "https://www.youtube.com/embed/tfiWaC-4UVA";
        }

        
        public string Url {
            get => _url;
            set => SetPropertyValue(nameof(Url), ref _url, value);
        }

        [EditorAlias(nameof(UploadFilePropertyEditor))]
        [Association("MyClass1-MyFiles")] public XPCollection<MyFile> MyFiles => GetCollection<MyFile>(nameof(MyFiles));
    }

    public class MyFile:FileData {
        public MyFile(Session session) : base(session) { }
        MyClass1 _myClass1;

        [Association("MyClass1-MyFiles")]
        public MyClass1 MyClass1 {
            get => _myClass1;
            set => SetPropertyValue(nameof(MyClass1), ref _myClass1, value);
        }
    }

    public class TestBlazorModule : ModuleBase,IWebModule{
        public TestBlazorModule(){
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Blazor.SystemModule.SystemBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.FileAttachments.Blazor.FileAttachmentsBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Validation.Blazor.ValidationBlazorModule));
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.JobSchedulerModule));
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Blazor.BlazorModule));
            RequiredModuleTypes.Add(typeof(TestApplicationModule));
        }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters) {
            base.AddGeneratorUpdaters(updaters);
            updaters.Add(new MyClass());
        }

        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.ConnectJobScheduler()
                .Subscribe(this);
        }
    }
}