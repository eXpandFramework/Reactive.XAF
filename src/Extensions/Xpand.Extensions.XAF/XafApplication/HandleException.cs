using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.Extensions.XAF.AppDomain;

namespace Xpand.Extensions.XAF.XafApplication{
    public static partial class XafApplicationExtensions{
        public static void HandleException(this DevExpress.ExpressApp.XafApplication application, System.Exception exception){
            Tracing.Tracer.LogError(exception);
            try{
                if (application.GetPlatform() == Platform.Win){
                    application.CallMethod("HandleException", exception);
                }
                else{
                    System.AppDomain.CurrentDomain.XAF().ErrorHandling().CallMethod("SetPageError", exception);
                }
            }
            catch (System.Exception e){
                Tracing.Tracer.LogError(e);
                throw;
            }
        }
    }
}