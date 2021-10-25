using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using TestApplication.Module.Blazor.JobScheduler;
using TestApplication.Module.Blazor.JobScheduler.Notification;
using Xpand.XAF.Modules.Blazor.Editors;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace TestApplication.Module.Blazor{
    [DefaultClassOptions]
    public class VideoObject:BaseObject {
        public VideoObject(Session session) : base(session) { }
        string _url;

        public string Url {
            get => _url;
            set {
                
                if (SetPropertyValue(nameof(Url), ref _url, value)) {
                    // Video = value;    
                }
            }
        }


        [PersistentAlias(nameof(Url))]
        
        
        [EditorAliasAttribute(nameof(PEditor))]
        
        public string Video => (string)EvaluateAlias(nameof(Video));
    }

    [PropertyEditor(typeof(string),nameof(PEditor),false)]
    public class PEditor:ComponentPropertyEditor {
        public PEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) { }

        protected override void RenderComponent(RenderTreeBuilder builder) {
            builder.AddMarkupContent(0, $"<iframe src='{PropertyValue}'/>");
        }
    }

    public class TestBlazorModule : ModuleBase,IWebModule{
        public TestBlazorModule(){
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Blazor.SystemModule.SystemBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.FileAttachments.Blazor.FileAttachmentsBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Validation.Blazor.ValidationBlazorModule));
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.JobSchedulerModule));
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.JobSchedulerNotificationModule));
            RequiredModuleTypes.Add(typeof(TestApplicationModule));
        }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters) {
            updaters.Add(new JoSchedulerSourceUpdater());
        }

        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.ConnectJobScheduler()
                .Merge(moduleManager.ConnectJobSchedulerNotification())
                .Subscribe(this);
        }
    }
}