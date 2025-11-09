using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.CloneObject;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Validation;
using DevExpress.ExpressApp.Validation.Win;
using DevExpress.Persistent.Validation;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.BulkObjectUpdate;
using Xpand.XAF.Modules.HideToolBar;
using Xpand.XAF.Modules.ModelViewInheritance;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;
using Xpand.XAF.Modules.Workflow.Services;
using Updater = Xpand.XAF.Modules.Workflow.DatabaseUpdate.Updater;

namespace Xpand.XAF.Modules.Workflow{
    
    public sealed class WorkflowModule : ReactiveModuleBase {
        static readonly List<Func<ApplicationModulesManager, IObservable<Unit>>> Connections =[
            manager => manager.WorkflowServiceConnect(),
            
            
        ];
        public WorkflowModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Win.SystemModule.SystemWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ViewItemValue.ViewItemValueModule));
            RequiredModuleTypes.Add(typeof(Telegram.TelegramModule));
            RequiredModuleTypes.Add(typeof(CloneModelView.CloneModelViewModule));
            RequiredModuleTypes.Add(typeof(CloneObjectModule));
            RequiredModuleTypes.Add(typeof(ModelViewInheritanceModule));
            RequiredModuleTypes.Add(typeof(BulkObjectUpdateModule));
            RequiredModuleTypes.Add(typeof(HideToolBarModule));
            RequiredModuleTypes.Add(typeof(ValidationModule));
            RequiredModuleTypes.Add(typeof(ValidationWindowsFormsModule));
        }


        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            new[]{typeof(CommandSortPropertyExistRule),typeof(StartCommandCircularDependencyRule)}
                .Do(type => ValidationRulesRegistrator.RegisterRule(moduleManager, type, typeof(IRuleBaseProperties) 
                )).Enumerate();
            
            Connections.ToNowObservable()
                .SelectMany(func => func(moduleManager))
                .Subscribe(this);
        }

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) 
            =>[new Updater(objectSpace, versionFromDB)];

    }
}
