using System;
using System.Diagnostics.CodeAnalysis;
using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.Reactive{
    internal partial class RxApp{
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateObject(IObjectSpace __instance,object __result){
	        NewObjectsSubject.OnNext(( __result,__instance));
        }

    }
}