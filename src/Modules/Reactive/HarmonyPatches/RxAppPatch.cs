using System.Diagnostics.CodeAnalysis;
using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.Reactive{
    
    internal static partial class RxApp{
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateModuleManager(ApplicationModulesManager __result){
            ApplicationModulesManagerSubject.OnNext(__result);
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateFrame(Frame __result){
            FramesSubject.OnNext(__result);
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateWindow(Window __result){
            FramesSubject.OnNext(__result);
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreatePopupWindow(Window __result){
            FramesSubject.OnNext(__result);
            PopupWindowsSubject.OnNext(__result);
        }     
    }
}