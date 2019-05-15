using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.Reactive{
    static partial class RxApp{
        static void CreateFrame(Frame __result){
            FramesSubject.OnNext(__result);
        }     
        static void CreatePopupWindow(Window __result){
            FramesSubject.OnNext(__result);
            PopupWindowsSubject.OnNext(__result);
        }     
    }
}