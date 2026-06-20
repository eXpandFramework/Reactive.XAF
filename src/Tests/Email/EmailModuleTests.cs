using System;
using System.Linq;
using System.Net.Mail;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;
using Shouldly;
using TestApplication.Blazor.Server.BusinessObjects;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Email.BusinessObjects;
using Xpand.XAF.Modules.Email.Tests.BOModel;
using Xpand.XAF.Modules.Email.Tests.Common;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Email.Tests {
    public class EmailModuleTests : EmailModuleTestsBaseTest {
        [Test]
        public async Task Run()
            => await StartEmailTest(application => application.WhenFirstFrame()
                .Merge(application.WhenSetupComplete().Do(_ => SetupModel(application)).To<Frame>().IgnoreElements()).Take(1)
                .Do(frame => {
                    var objectSpace = frame.View.ObjectSpace;
                    var user = (ApplicationUser)objectSpace.GetObject(SecuritySystem.CurrentUser);
                    user.SetMemberValue("Email","mail@mail.com");
                    objectSpace.CreateObject<E>().CommitChanges();
                })
                .SelectMany(_ => application.Navigate(typeof(E))
                    .SelectMany(frame => frame.AssertListViewHasObject<E>()))
                .SelectMany(frame => Activate_EmailAction_When_Rule_Exists(application, frame)
                    .Merge(frame.ListViewProcessSelectedItem(frame.View.Objects().First).IgnoreElements()))
                .Take(1).ToUnit());

        private static void SetupModel(BlazorApplication application){
            var modelEmail = application.Model.ToReactiveModule<IModelReactiveModulesEmail>().Email;
            application.Model.Title = nameof(EmailModuleTests);
            var recipientType = modelEmail.RecipientTypes.AddNode<IModelEmailRecipientType>();
            recipientType.Type = modelEmail.Application.BOModel.GetClass(typeof(ApplicationUser));
            recipientType.EmailMember = recipientType.Type.FindMember("Email");
            var emailAddress = modelEmail.EmailAddress.AddNode<IModelEmailAddress>();
            emailAddress.Address = "mail@mail.com";
            var smtpClient = modelEmail.SmtpClients.AddNode<IModelEmailSmtpClient>();
            smtpClient.From = emailAddress;
            smtpClient.Host = "mail.mail.com";
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            var modelEmailRule = modelEmail.Rules.AddNode<IModelEmailRule>();
            var emailObjectView = modelEmailRule.ObjectViews.AddNode<IModelEmailObjectView>();
            emailObjectView.ObjectView =
                emailObjectView.Application.BOModel.GetClass(typeof(E)).DefaultDetailView;
            emailObjectView.Subject = emailObjectView.ObjectView.ModelClass.FindMember(nameof(E.Name));
            var emailRecipient = modelEmail.Recipients.AddNode<IModelEmailRecipient>();
            emailRecipient.RecipientType = recipientType;
            var viewRecipient = modelEmailRule.ViewRecipients.AddNode<IModelEmailViewRecipient>();
            viewRecipient.ObjectView = emailObjectView;
            viewRecipient.Recipient = emailRecipient;
            viewRecipient.SmtpClient = smtpClient;
        }

        private IObservable<Frame> Activate_EmailAction_When_Rule_Exists(BlazorApplication application, Frame frame)
            => application.WhenFrame(typeof(E), ViewType.DetailView).Take(1)
                .SelectMany(frame1 => frame1.View.WhenControlsCreated().To(frame1).Take(1))
                .SelectMany(frame1 => frame1.AssertSingleChoiceAction(nameof(EmailService.Email),action => {
                    action.Active.ResultValue.ShouldBeTrue();
                    action.Enabled.ResultValue.ShouldBeTrue();
                    return 2;
                })
                .SelectMany(action => Send_Email(action))
                .DoOnComplete(() => frame1.View.Close()))
                .Delay(1.ToSeconds()).ObserveOnContext()
                .To(frame);

        private static IObservable<Frame> Send_Email( SingleChoiceAction action){
            var modelEmail = action.Application.Model.ToReactiveModule<IModelReactiveModulesEmail>().Email;
            var viewRecipient = modelEmail.Rules.First().ViewRecipients.First();
            var detailView = (DetailView)action.View();
            using var testObserver = action.Application.WhenSendingEmail().TakeFirst().Test();

            action.DoExecute(_ => new[] { detailView.CurrentObject });

            testObserver.AwaitDone(AssertExtensions.TimeoutInterval).ItemCount.ShouldBe(1);
            var id = viewRecipient.Id();
            action.Application.WhenCommitted<EmailStorage>().SelectMany(t => t.objects)
                .TakeFirst(storage
                    => storage.ViewRecipient == id &&
                       storage.Key == ((E)detailView.CurrentObject).Oid.ToString())
                .Timeout(AssertExtensions.TimeoutInterval);
            return action.Frame().Observe();
        }
    }
}