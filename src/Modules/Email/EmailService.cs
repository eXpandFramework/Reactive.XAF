using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reactive;
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
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Email{
    public static class EmailService {
        public static SingleChoiceAction Email(this (EmailModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(Email)).As<SingleChoiceAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.RegisterAction().AddItems(action => action.AddItems().ToUnit()).SendEmail();

        private static IObservable<Unit> SendEmail(this IObservable<SingleChoiceAction> source)
            => source.Select(action => action)
                .WhenExecuted(e => e.Action.ViewRecipients().Where(t => t.receipient==e.SelectedChoiceActionItem.Data)
                .SelectMany(t => {
                    var modelEmail = e.Action.Model.Application.ModelEmail();
                    var sendToAddresses = e.Action.View().ObjectSpace.GetObjects(SecuritySystem.UserType,
                            CriteriaOperator.Parse(t.receipient.Recipient.UserCriteria)).Cast<object>()
                        .Select(o => new MailAddress($"{modelEmail.UserEmailMember.MemberInfo.GetValue(o)}")).ToArray();
                    return sendToAddresses.Any() ? e.SendEmail(t.receipient,  sendToAddresses) : Observable.Empty<Unit>();
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
                    return message.SendEmail(recipient,  o);
                }))
                .ToUnit();

        private static IObservable<MailMessage> SendEmail(this MailMessage message,IModelEmailViewRecipient recipient,  object o) {
            var customizeSend = message.NewSmtpClient(o, recipient);
            return customizeSend.client.WhenEvent<AsyncCompletedEventArgs>(nameof(customizeSend.client.SendCompleted))
                .SelectMany(e => e.Error != null ? Observable.Throw<AsyncCompletedEventArgs>(e.Error)
                        : Observable.Empty<AsyncCompletedEventArgs>()).ToUnit().Merge(customizeSend.client
                    .SendMailAsync(customizeSend.message).ToObservable())
                .Finally(() => customizeSend.client.Dispose()).To(message).TraceEmail();
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
            if (!string.IsNullOrEmpty(modelSmtpClient.PickupDirectoryLocation))
                smtpClient.PickupDirectoryLocation = Path.GetFullPath(Environment.ExpandEnvironmentVariables(modelSmtpClient.PickupDirectoryLocation));
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
                .Where(receipient => receipient.ObjectView.ObjectView==action.View().Model)
                .Select(receipient => (receipient,rule))).ToNowObservable();

        private static IObservable<SingleChoiceAction> RegisterAction(this ApplicationModulesManager manager) 
            => manager.RegisterViewSingleChoiceAction(nameof(Email),action => action.ConfigureAction());

        private static void ConfigureAction(this SingleChoiceAction action) {
            action.SelectionDependencyType=SelectionDependencyType.RequireMultipleObjects;
            action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
            action.ImageName = "Actions_Send";
        }

        private static IObservable<ChoiceActionItem> AddItems(this SingleChoiceAction action) 
            => action.Model.Application.ModelEmail().Rules
                .SelectMany(rule => rule.ViewRecipients).Where(viewReceipient => viewReceipient.ObjectView.ObjectView == action.View().Model)
                .Select(viewReceipient => new ChoiceActionItem(viewReceipient.Caption, viewReceipient)).ToNowObservable()
                .Do(item => action.Items.Add(item)).TraceEmail(item => item.Caption);
        
        internal static IObservable<TSource> TraceEmail<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, EmailModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
        


        
        

    }
}