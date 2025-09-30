using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.SuppressConfirmation;
using Xpand.XAF.Modules.Telegram.Services;
using Updater = Xpand.XAF.Modules.Telegram.DatabaseUpdate.Updater;

namespace Xpand.XAF.Modules.Telegram{
    
    public sealed class TelegramModule : ModuleBase {
        static readonly List<Func<ApplicationModulesManager, IObservable<Unit>>> Connections =[
            manager => manager.TelegramBotConnect(),
            manager => manager.TelegramChatConnect(),
            
        ];
        public TelegramModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(CloneModelViewModule));
            RequiredModuleTypes.Add(typeof(SuppressConfirmationModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            Connections.ToNowObservable()
                .SelectMany(func => func(moduleManager))
                .Subscribe(this);
        }

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) 
            =>[new Updater(objectSpace, versionFromDB)];

    }
}
