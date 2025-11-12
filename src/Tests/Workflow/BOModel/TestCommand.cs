using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;

namespace Xpand.XAF.Modules.Workflow.Tests.BOModel{
    [DefaultProperty(nameof(Id))]
    public class TestCommand(Session session) :WorkflowCommand(session) {
        
        bool _shouldFail;

        [Browsable(false)]
        public bool ShouldFail {
            get => _shouldFail;
            set => SetPropertyValue(nameof(ShouldFail), ref _shouldFail, value);
        }

        public override IObservable<object[]> Execute(XafApplication application, params object[] objects) {
            

            if (ShouldFail) {
                return Observable.Throw<object[]>(new InvalidOperationException("Execution Failure"));
            }
        
            if (Id == "CommandA") {
                return new object[] { 42, "test-string" }.Observe();
            }
        
            if (Id == "CombinedFilterTrigger") {
                var oids = ObjectSpace.GetObjectsQuery<WF>()
                    .Where(wf => wf.Status == "Active" || wf.Status == "Inactive")
                    .Select(wf => wf.Oid)
                    .Cast<object>().ToArray();
                return new[] { oids }.ToObservable();
            }
            return new[] { Array.Empty<object>()
                    .Concat(string.Join(";", objects.Select(o => o?.ToString() ?? "null")).YieldItem().WhereNotNullOrEmpty()) }.SelectMany().ToArray()
                .Observe();
        }
        protected override bool GetNeedSubscription() => Subscription;

        bool _subscription;

        [Browsable(false)]
        public bool Subscription {
            get => _subscription;
            set => SetPropertyValue(nameof(Subscription), ref _subscription, value);
        }
        
        string _id;

        public string Id {
            get => _id;
            set => SetPropertyValue(nameof(Id), ref _id, value);
        }

        
    }
}