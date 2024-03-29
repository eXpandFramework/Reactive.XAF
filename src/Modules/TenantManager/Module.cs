﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Blazor;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.TenantManager{
    
    public sealed class TenantManagerModule : ReactiveModuleBase{
        
        
        public static ReactiveTraceSource TraceSource{ get; set; }
        static TenantManagerModule(){
            TraceSource=new ReactiveTraceSource(nameof(TenantManagerModule));
        }
        
        public TenantManagerModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            RequiredModuleTypes.Add(typeof(BlazorModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
	            .Merge(Application.WhenViewCreated().SelectMany(view => view.WhenCustomizeViewShortcut()).ToUnit())
                .TakeUntilDisposed(this)
                .Subscribe();
        }
        
        public override IList<PopupWindowShowAction> GetStartupActions() => Application.StartupActions(base.GetStartupActions());

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModulesTenantManager>();
        }
    }
}