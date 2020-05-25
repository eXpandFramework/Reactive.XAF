using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;

using Xpand.XAF.Modules.LookupCascade.Tests.BOModel;

namespace Xpand.XAF.Modules.LookupCascade.Tests{
    public abstract class LookupCascadeBaseTest:BaseTest{
        protected static LookupCascadeModule ClientLookupCascadeModule(params ModuleBase[] modules){
            var xafApplication = Platform.Web.NewApplication<LookupCascadeModule>();
            xafApplication.Modules.AddRange(modules);
            var module = xafApplication.AddModule<LookupCascadeModule>(typeof(Product),typeof(Order),typeof(Accessory));
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return module.Application.Modules.OfType<LookupCascadeModule>().First();
        }
    }
}