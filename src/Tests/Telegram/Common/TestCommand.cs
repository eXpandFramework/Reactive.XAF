using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;

namespace Xpand.XAF.Modules.Telegram.Tests.Common{
    public class TestCommand(Session session) :WorkflowCommand(session) {
        bool _returnEmpty;

        [Browsable(false)]
        public bool ReturnEmpty {
            get => _returnEmpty;
            set => SetPropertyValue(nameof(ReturnEmpty), ref _returnEmpty, value);
        }
        string _outputMessages;

        [Browsable(false)]
        public string OutputMessages {
            get => _outputMessages;
            set => SetPropertyValue(nameof(OutputMessages), ref _outputMessages, value);
        }

        public override IObservable<object[]> Execute(XafApplication application, params object[] objects) {
            if (ReturnEmpty) {
                return Observable.Return(Array.Empty<object>());
            }
            if (OutputMessages != null) {
                return Observable.Return(OutputMessages.Split(';').Cast<object>().ToArray());
            }
            return new object[] { "Hello from Workflow" }.Observe();
        }
        
        protected override bool GetNeedSubscription() => false;
    }
}