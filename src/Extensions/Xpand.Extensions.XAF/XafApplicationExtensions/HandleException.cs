using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.Extensions.XAF.AppDomainExtensions;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public static partial class XafApplicationExtensions{
        public static void HandleException(this DevExpress.ExpressApp.XafApplication application, System.Exception exception){
            Tracing.Tracer.LogError(exception);
            try{
	            var platform = application.GetPlatform();
	            if (platform == Platform.Win){
                    application.CallMethod("HandleException", exception);
                }
                else if (platform == Platform.Web){
                    System.AppDomain.CurrentDomain.XAF().ErrorHandling().CallMethod("SetPageError", exception);
                }
	            else{
		            throw exception;
	            }
            }
            catch (System.Exception e){
                Tracing.Tracer.LogError(e);
                throw;
            }
        }
    }
}