using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Email.BusinessObjects;
using Xpand.XAF.Modules.Email.Tests.BOModel;
using Xpand.XAF.Modules.Email.Tests.Common;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;


namespace Xpand.XAF.Modules.Email.Tests {
    public class EmailModuleTests : CommonAppTest {
        
        private IModelEmailViewRecipient _viewRecipient;

        public override void Init() {
            ReactiveModuleBase.Scheduler=TestScheduler;
            base.Init();
            var modelEmail = Application.Model.ToReactiveModule<IModelReactiveModulesEmail>().Email;
            Application.Model.Title = nameof(EmailModuleTests);
            var recipientType = modelEmail.RecipientTypes.AddNode<IModelEmailRecipientType>();
            recipientType.Type = modelEmail.Application.BOModel.GetClass(typeof(EmailUser));
            recipientType.EmailMember = recipientType.Type.FindMember(nameof(EmailUser.Email));
            var emailAddress = modelEmail.EmailAddress.AddNode<IModelEmailAddress>();
            emailAddress.Address = "mail@mail.com";
            var smtpClient = modelEmail.SmtpClients.AddNode<IModelEmailSmtpClient>();
            smtpClient.From=emailAddress;
            smtpClient.Host = "mail.mail.com";
            var modelEmailRule = modelEmail.Rules.AddNode<IModelEmailRule>();
            var emailObjectView = modelEmailRule.ObjectViews.AddNode<IModelEmailObjectView>();
            emailObjectView.ObjectView = emailObjectView.Application.BOModel.GetClass(typeof(E)).DefaultDetailView;
            emailObjectView.Subject = emailObjectView.ObjectView.ModelClass.FindMember(nameof(E.Name));
            var emailRecipient = modelEmail.Recipients.AddNode<IModelEmailRecipient>();
            emailRecipient.RecipientType=recipientType;
            _viewRecipient = modelEmailRule.ViewRecipients.AddNode<IModelEmailViewRecipient>();
            _viewRecipient.ObjectView=emailObjectView;
            _viewRecipient.Recipient = emailRecipient;
            _viewRecipient.SmtpClient=smtpClient;
            
        }

        [Test][Order(0)]
        public void Activate_EmailAction_When_Rule_Exists() {
            using var window = Application.CreateViewWindow();
            var detailView = Application.NewView<DetailView>(typeof(E));
            detailView.CurrentObject = detailView.ObjectSpace.CreateObject<E>();
            window.SetView(detailView);
            TestScheduler.AdvanceTimeBy(2.Seconds());
            window.Action(nameof(EmailService.Email)).Active.ResultValue.ShouldBeTrue();
            window.Action(nameof(EmailService.Email)).Enabled.ResultValue.ShouldBeTrue();
            
            window.Close();
            
            
        }
        [Test][Order(100)]
        public void Disable_EmailAction_When_Email_Already_Sent() {
            _viewRecipient.ObjectView.UniqueSend = true;
            var objectSpace = Application.CreateObjectSpace();
            var e = objectSpace.CreateObject<E>();
            objectSpace.CommitChanges();
            var emailStorage = objectSpace.CreateObject<EmailStorage>();
            emailStorage.Key = e.Oid.ToString();
            emailStorage.ViewRecipient = _viewRecipient.Id();
            objectSpace.CommitChanges();
            using var window = Application.CreateViewWindow();
            var detailView = Application.NewView<DetailView>(typeof(E));
            detailView.CurrentObject = detailView.ObjectSpace.GetObject(e);
            window.SetView(detailView);
            TestScheduler.AdvanceTimeBy(2.Seconds());
            window.Action(nameof(EmailService.Email)).Enabled["DisableIfSent"].ShouldBeFalse();
            
            emailStorage.ObjectSpace.Delete(emailStorage);
            emailStorage.ObjectSpace.CommitChanges();
            window.Close();
            _viewRecipient.ObjectView.UniqueSend = false;
        }
        [Test][Order(200)]
        public void Send_Email() {
            var window = Application.CreateViewWindow();
            var detailView = Application.NewView<DetailView>(typeof(E));
            var e = detailView.ObjectSpace.CreateObject<E>();
            detailView.CurrentObject = e;
            window.SetView(detailView);
            TestScheduler.AdvanceTimeBy(2.Seconds());
            using var testObserver = Application.WhenSendingEmail().TakeFirst().Test();
            var action = window.Action(nameof(EmailService.Email));
            
            action.DoExecute(_ => new []{detailView.CurrentObject});
            
            testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
            var id = _viewRecipient.Id();
            Application.WhenCommitted<EmailStorage>().SelectMany(t => t.objects)
                .TakeFirst(storage => storage.ViewRecipient == id && storage.Key == e.Oid.ToString()).Timeout(Timeout);
        }


    }
}
