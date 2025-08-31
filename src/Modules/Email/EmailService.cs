using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Email.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Email{
    public static class EmailService {
        private static IScheduler Scheduler => ReactiveModuleBase.Scheduler;
        public static SingleChoiceAction Email(this (EmailModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(Email)).As<SingleChoiceAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.RegisterAction()
                .AddItems(action => action.AddItems().ToUnit()
                .Concat(Observable.Defer(() => action.DisableIfSent().SendEmail())),Scheduler).ToUnit();

        private static IObservable<SingleChoiceAction> DisableIfSent(this SingleChoiceAction singleChoiceAction) {
            var view = singleChoiceAction.View();
            singleChoiceAction.DisableIfSent( view);
            return view.WhenSelectionChanged().Do(singleChoiceAction.DisableIfSent).To(singleChoiceAction)
                .Merge(singleChoiceAction.WhenExecuteFinished().Do(action => action.DisableIfSent()))
                .IgnoreElements().StartWith(singleChoiceAction).WhenActive();
        }

        private static void DisableIfSent(this SingleChoiceAction singleChoiceAction, View view) {
            var objectSpace = view.ObjectSpace;
            var recipients = singleChoiceAction.Items.Select(item => item.Data).Cast<IModelEmailViewRecipient>()
                .Select(recipient => recipient.Id()).ToArray();
            var selectedObjects = view.SelectedObjects.Cast<object>().ToArray();
            var sendCount = selectedObjects.Select(o => $"{view.ObjectSpace.GetKeyValue(o)}").Where(s => s!=String.Empty)
                .SelectMany(key => objectSpace.GetObjectsQuery<EmailStorage>()
                    .Where(storage => storage.Key == key && recipients.Contains(storage.ViewRecipient))).Count() ;
            singleChoiceAction.Enabled[nameof(DisableIfSent)] = selectedObjects.Length>sendCount;
        }

        private static IObservable<Unit> SendEmail(this IObservable<SingleChoiceAction> source)
            => source.WhenExecuted(e => e.Action.ViewRecipients().Where(t => t.receipient==e.SelectedChoiceActionItem.Data)
                .SelectMany(t => {
                    var sendToAddresses = e.Action.View().ObjectSpace.GetObjects(SecuritySystem.UserType,
                            CriteriaOperator.Parse(t.receipient.Recipient.RecipientTypeCriteria)).Cast<object>()
                        .Select(o => new MailAddress($"{t.receipient.Recipient.RecipientType.EmailMember.MemberInfo.GetValue(o)}")).ToArray();
                    return sendToAddresses.Any() ? e.SendEmail(t.receipient,  sendToAddresses).Do(_ => e.Action.ExecutionFinished()) : Observable.Empty<Unit>();
                }));

        private static IObservable<Unit> SendEmail(this SingleChoiceActionExecuteEventArgs e,
            IModelEmailViewRecipient recipient, MailAddress[] users) 
            => e.SelectedObjects.Cast<object>().ToNowObservable()
                .SelectMany(o => Observable.Using(() => new MailMessage(), message => {
                    message.Subject = $"{recipient.ObjectView.Subject.MemberInfo.GetValue(o)}";
                    message.Body = $"{recipient.ObjectView.Body?.MemberInfo.GetValue(o)}";
                    message.From = new MailAddress(recipient.SmtpClient.From.Address);
                    message.IsBodyHtml = true;
                    recipient.SmtpClient.ReplyTo.ForEach(address => message.ReplyToList.Add(new MailAddress(address.EmailAddress.Address)));
                    users.ForEach(address => message.To.Add(address));
                    return message.SendEmail(recipient,  o)
                        .Store(e.Action.Application);
                }))
                .ToUnit();

        private static IObservable<Unit> Store(this IObservable<(IModelEmailViewRecipient recipient,object o)> source,XafApplication application) 
            => source.SelectMany(t => Observable.Using(application.CreateObjectSpace, space => {
                var emailStorage = space.CreateObject<EmailStorage>();
                emailStorage.Key = $"{space.GetKeyValue(t.o)}";
                emailStorage.ViewRecipient = t.recipient.Id();
                space.CommitChanges();
                return Unit.Default.Observe();
            }));

        private static IObservable<(IModelEmailViewRecipient recipient,object o)> SendEmail(this MailMessage message,IModelEmailViewRecipient recipient,  object o) {
            var customizeSend = message.NewSmtpClient(o, recipient);
            return customizeSend.client.ProcessEvent<AsyncCompletedEventArgs>(nameof(customizeSend.client.SendCompleted))
                .SelectMany(e => e.Error != null ? Observable.Throw<(IModelEmailViewRecipient,object)>(e.Error)
                        : Observable.Empty<(IModelEmailViewRecipient,object)>())
                .Merge(customizeSend.client.SendMailAsync(customizeSend.message).ToObservable().Select(_ => (recipient,o)))
                .Finally(() => customizeSend.client.Dispose()).Select(_ => (recipient,o)).TraceEmail();
        }

        private static readonly Subject<GenericEventArgs<(SmtpClient client,MailMessage message,object bo,string viewId)>> CustomizeSendSubject = new();

        public static IObservable<GenericEventArgs<(SmtpClient client, MailMessage message, object o, string viewId)>>
            WhenSendingEmail(this XafApplication application) 
            => CustomizeSendSubject;
        
        private static (SmtpClient client, MailMessage message, object bo, string viewId) NewSmtpClient(
            this MailMessage mailMessage, object o, IModelEmailViewRecipient emailViewRecipient) {
            var modelSmtpClient = emailViewRecipient.SmtpClient;
            var smtpClient = new SmtpClient {
                DeliveryMethod = modelSmtpClient.DeliveryMethod,
            };
            if (!string.IsNullOrEmpty(modelSmtpClient.PickupDirectoryLocation)) {
                smtpClient.PickupDirectoryLocation = Path.GetFullPath(Environment.ExpandEnvironmentVariables(modelSmtpClient.PickupDirectoryLocation));
            }
            if (smtpClient.DeliveryMethod == SmtpDeliveryMethod.Network){
                smtpClient.Host = modelSmtpClient.Host;
                smtpClient.Port = modelSmtpClient.Port;
                smtpClient.EnableSsl = modelSmtpClient.EnableSsl;
                smtpClient.UseDefaultCredentials = modelSmtpClient.UseDefaultCredentials;
                if (!smtpClient.UseDefaultCredentials){
                    smtpClient.Credentials = new NetworkCredential(modelSmtpClient.UserName.Address,
                        modelSmtpClient.Password);
                }
            }
            else{
                if (!string.IsNullOrEmpty(smtpClient.PickupDirectoryLocation) &&
                    !Directory.Exists(smtpClient.PickupDirectoryLocation)){
                    Directory.CreateDirectory(smtpClient.PickupDirectoryLocation);
                }
            }
            var e = new GenericEventArgs<(SmtpClient client,MailMessage message,object bo,string viewId)>((smtpClient,mailMessage,o,emailViewRecipient.ObjectView.Id()));
            CustomizeSendSubject.OnNext(e);
            return e.Instance;
        }

        private static IObservable<(IModelEmailViewRecipient receipient, IModelEmailRule rule)> ViewRecipients(this ActionBase action) 
            => action.Model.Application.ModelEmail().Rules.SelectMany(rule => rule.ViewRecipients
                .Where(recipient => recipient.ObjectView.ObjectView==action.View().Model)
                .Select(recipient => (receipient: recipient,rule))).ToNowObservable();

        private static IObservable<SingleChoiceAction> RegisterAction(this ApplicationModulesManager manager) 
            => manager.RegisterViewSingleChoiceAction(nameof(Email),action => action.ConfigureAction());

        private static void ConfigureAction(this SingleChoiceAction action) {
            action.SelectionDependencyType=SelectionDependencyType.RequireMultipleObjects;
            action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
            action.ImageName = "Actions_Send";
            action.CustomizeExecutionFinished();
        }

        private static IObservable<ChoiceActionItem> AddItems(this SingleChoiceAction action) 
            => action.Model.Application.ModelEmail().Rules
                .SelectMany(rule => rule.ViewRecipients).Where(viewRecipient => viewRecipient.ObjectView.ObjectView == action.View().Model)
                .Select(viewRecipient => new ChoiceActionItem(viewRecipient.Caption, viewRecipient)).ToNowObservable()
                .Do(item => action.Items.Add(item)).TraceEmail(item => item.Caption);
        
        internal static IObservable<TSource> TraceEmail<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, EmailModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);
        
        


        
        

    }
}