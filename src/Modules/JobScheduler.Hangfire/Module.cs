using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Validation;
using DevExpress.ExpressApp.Validation.Blazor;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Xpand.XAF.Modules.Blazor;
using Xpand.XAF.Modules.Blazor.Editors;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    public sealed class JobSchedulerModule : ReactiveModuleBase {

        static JobSchedulerModule() => TraceSource=new ReactiveTraceSource(nameof(JobSchedulerModule));

        
        public static ReactiveTraceSource TraceSource{ get; set; }
        public JobSchedulerModule() {
            RequiredModuleTypes.Add(typeof(BlazorModule));
            // RequiredModuleTypes.Add(typeof(SystemBlazorModule));
            RequiredModuleTypes.Add(typeof(ValidationModule));
            RequiredModuleTypes.Add(typeof(ValidationBlazorModule));
            RequiredModuleTypes.Add(typeof(ConditionalAppearanceModule));
        }

        protected override void RegisterEditorDescriptors(EditorDescriptorsFactory editorDescriptorsFactory) {
            base.RegisterEditorDescriptors(editorDescriptorsFactory);
            editorDescriptorsFactory.RegisterPropertyEditor(nameof(DisplayTextPropertyEditor),typeof(object),typeof(DisplayTextPropertyEditor),false);
            editorDescriptorsFactory.RegisterPropertyEditorAlias(nameof(DisplayTextPropertyEditor),typeof(object),false);
        }

        public override void Setup(XafApplication application) {
            base.Setup(application);
            JobSchedulerService.CustomJobSchedule
                .Do(e => {
                    e.Handled = true;
                    e.SetInstance(bus => bus.Do(job => job.ObjectSpace.ServiceProvider
                        .GetService<IRecurringJobManager>()
                        .AddOrUpdate(job.Id, job.Expression(), () => job.CronExpression?.Expression ?? Cron.Never())));
                })
                .Subscribe(this);
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) 
            => base.GetModuleUpdaters(objectSpace, versionFromDB).Concat(new []{new CronExpressionModuleUpdater(objectSpace, versionFromDB,Application) });

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModulesJobScheduler>();
        }

    }
}
