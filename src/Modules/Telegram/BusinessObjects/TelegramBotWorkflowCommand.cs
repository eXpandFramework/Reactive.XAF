using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Telegram.Services;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;

namespace Xpand.XAF.Modules.Telegram.BusinessObjects{
    public class TelegramBotWorkflowCommand(Session session) :WorkflowCommand(session) {
        public override IObservable<object[]> Execute(XafApplication application, params object[] objects) 
            => TelegramBot is not { Active: true } ? Observable.Empty<object[]>()
                : objects.Observe().WhenNotEmpty()
                    .SelectMany(_ => TelegramBot.SendText(objects.WhereNotDefault().Select(o => o.ToString()).ToArray()))
                    .Select(VAR => VAR)
                    .To<object[]>();

        TelegramBot _telegramBot;

        [RuleRequiredField][DataSourceCriteria(nameof(BusinessObjects.TelegramBot.Active)+"=1")]
        public TelegramBot TelegramBot {
            get => _telegramBot;
            set => SetPropertyValue(nameof(TelegramBot), ref _telegramBot, value);
        }
    }
}