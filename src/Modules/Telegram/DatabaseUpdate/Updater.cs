using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Xpand.XAF.Modules.Telegram.DatabaseUpdate{
    
    public class Updater(IObjectSpace objectSpace, Version currentDBVersion)
        : ModuleUpdater(objectSpace, currentDBVersion){


        
    }
}