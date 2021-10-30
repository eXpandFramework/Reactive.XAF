using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Email.Tests.BOModel;
using Xpand.XAF.Modules.Email.Tests.Common;
using Xpand.XAF.Modules.Reactive;


namespace Xpand.XAF.Modules.Email.Tests {
    public class EmailModuleTests : CommonAppTest {
        public override void Init() {
            base.Init();
            var modelEmail = Application.Model.ToReactiveModule<IModelReactiveModulesEmail>().Email;
            Application.Model.Title = nameof(EmailModuleTests);
            modelEmail.UserType = modelEmail.Application.BOModel.GetClass(typeof(EmailUser));
            modelEmail.UserEmailMember = modelEmail.UserType.FindMember(nameof(EmailUser.Email));
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
            var viewRecipient = modelEmailRule.ViewRecipients.AddNode<IModelEmailViewRecipient>();
            viewRecipient.ObjectView=emailObjectView;
            viewRecipient.Recipient = emailRecipient;
            viewRecipient.SmtpClient=smtpClient;
        }

        [Test][Order(0)]
        public void Activate_EmailAction_When_Rule_Exists() {
            using var window = Application.CreateViewWindow();
            window.SetView(Application.NewView<DetailView>(typeof(E)));
            
            window.Action(nameof(EmailService.Email)).Active.ResultValue.ShouldBeTrue();
            
            window.Close();
        }
        [Test][Order(100)]
        public void Send_Email() {
            var window = Application.CreateViewWindow();
            var detailView = Application.NewView<DetailView>(typeof(E));
            detailView.CurrentObject = detailView.ObjectSpace.CreateObject<E>();
            window.SetView(detailView);
            Application.WhenSendingEmail()
                .Do(e => {
                    var smtpClient = e.Instance.client; //configure the client
                    var mailMessage = e.Instance.message; //configure the message
                }).Subscribe();
            using var testObserver = Application.WhenSendingEmail().FirstAsync().Test();
            var action = window.Action(nameof(EmailService.Email));
            
            action.DoExecute(_ => new []{detailView.CurrentObject});
            
            testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
        }







    }
}
