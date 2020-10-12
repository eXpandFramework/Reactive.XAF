using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.ExpressApp;

namespace ALL.Web.Tests
{
    public static class SequenceGeneratorService{
        public static IObservable<Unit> Connect(this ApplicationModulesManager manager){
            return Observable.Empty<Unit>();
        }

    }
}
