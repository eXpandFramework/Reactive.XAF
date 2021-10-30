using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Email;
using Xpand.XAF.Modules.RazorView.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Module.Email {
    public static class EmailService {
        public static IObservable<Unit> ConnectEmail(this ApplicationModulesManager manager) {
            return manager.WhenGeneratingModelNodes<IModelBOModel>()
                .Do(model => {
                    var modelEmail = model.Application.ToReactiveModule<IModelReactiveModulesEmail>().Email;
                    modelEmail.SetupUser();
                    var emailAddress = modelEmail.SetupEmailAddress();
                    var smtpClient = modelEmail.SmtpClient(emailAddress);
                    var modelEmailRule = modelEmail.EmailRule();
                    var emailRecipient = modelEmail.EmailRecipient();
                    var emailObjectView = modelEmailRule.ObjectView(nameof(RazorView.Template));
                    modelEmailRule.SetupViewRecipient( emailObjectView, emailRecipient, smtpClient);
                    emailObjectView = modelEmailRule.ObjectView(nameof(RazorView.Preview));
                    modelEmailRule.SetupViewRecipient( emailObjectView, emailRecipient, smtpClient);
                }).ToUnit();
        }

        private static IModelEmailRule EmailRule(this IModelEmail modelEmail) {
            var modelEmailRule = modelEmail.Rules.AddNode<IModelEmailRule>();
            modelEmailRule.Type = modelEmail.Application.BOModel.GetClass(typeof(RazorView));
            ((ModelNode)modelEmailRule).Id = "RazorView rules";
            return modelEmailRule;
        }

        private static IModelEmailObjectView ObjectView(this IModelEmailRule modelEmailRule, string body) {
            var emailObjectView = modelEmailRule.ObjectViews.AddNode<IModelEmailObjectView>();
            emailObjectView.ObjectView = emailObjectView.Application.BOModel.GetClass(typeof(RazorView)).DefaultDetailView;
            emailObjectView.Subject = emailObjectView.ObjectView.ModelClass.FindMember(nameof(RazorView.Name));
            emailObjectView.Body = emailObjectView.ObjectView.ModelClass.FindMember(body);
            ((ModelNode)emailObjectView).Id = body;
            return emailObjectView;
        }

        private static void SetupViewRecipient(this IModelEmailRule modelEmailRule, IModelEmailObjectView emailObjectView,
            IModelEmailRecipient emailRecipient, IModelEmailSmtpClient smtpClient) {
            var viewRecipient = modelEmailRule.ViewRecipients.AddNode<IModelEmailViewRecipient>();
            viewRecipient.ObjectView = emailObjectView;
            viewRecipient.Recipient = emailRecipient;
            viewRecipient.SmtpClient = smtpClient;
            ((ModelNode)viewRecipient).Id = viewRecipient.Caption;
        }

        private static IModelEmailRecipient EmailRecipient(this IModelEmail modelEmail) {
            var emailRecipient = modelEmail.Recipients.AddNode<IModelEmailRecipient>();
            emailRecipient.UserCriteria = "[Roles][StartsWith([Name], 'Admin')]";
            ((ModelNode)emailRecipient).Id = "Admins";
            return emailRecipient;
        }

        private static IModelEmailAddress SetupEmailAddress(this IModelEmail modelEmail) {
            var emailAddress = modelEmail.EmailAddress.AddNode<IModelEmailAddress>();
            emailAddress.Address = "expandframework@gmail.com";
            return emailAddress;
        }

        private static void SetupUser(this IModelEmail modelEmail) {
            modelEmail.UserType = modelEmail.Application.BOModel.GetClass(typeof(User));
            modelEmail.UserEmailMember = modelEmail.UserType.FindMember(nameof(User.Email));
        }

        private static IModelEmailSmtpClient SmtpClient(this IModelEmail modelEmail, IModelEmailAddress emailAddress) {
            var smtpClient = modelEmail.SmtpClients.AddNode<IModelEmailSmtpClient>();
            smtpClient.From = emailAddress;
            smtpClient.Host = "smtp.gmail.com";
            smtpClient.Port = 587;
            smtpClient.EnableSsl = true;
            smtpClient.UserName=emailAddress;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Password = "89#AgCkOKbCW";
            ((ModelNode)smtpClient).Id = "smtp.gmail.com";
            var emailAddressesDep = smtpClient.ReplyTo.AddNode<IModelEmailAddressesDep>();
            emailAddressesDep.EmailAddress=emailAddress;
            
            return smtpClient;
        }
    }
}