using System;
using System.Net.Mail;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.Email;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Module.Email {
    public static class EmailService {
        public static IObservable<Unit> ConnectEmail(this ApplicationModulesManager manager) {
            return manager.WhenGeneratingModelNodes<IModelBOModel>()
                .Do(model => {
                    var modelEmail = model.Application.ToReactiveModule<IModelReactiveModulesEmail>().Email;
                    var recipientType = modelEmail.RecipientType();
                    var emailAddress = modelEmail.SetupEmailAddress();
                    var smtpClient = modelEmail.SmtpClient(emailAddress);
                    var modelEmailRule = modelEmail.EmailRule();
                    var emailRecipient = modelEmail.EmailRecipient(recipientType);
                    var emailObjectView = modelEmailRule.ObjectView(nameof(Product.ProductName));
                    modelEmailRule.SetupViewRecipient( emailObjectView, emailRecipient, smtpClient);
                }).ToUnit();
        }

        private static IModelEmailRule EmailRule(this IModelEmail modelEmail) {
            var modelEmailRule = modelEmail.Rules.AddNode<IModelEmailRule>();
            modelEmailRule.Type = modelEmail.Application.BOModel.GetClass(typeof(Product));
            ((ModelNode)modelEmailRule).Id = $"{nameof(Product)} rules";
            return modelEmailRule;
        }

        private static IModelEmailObjectView ObjectView(this IModelEmailRule modelEmailRule, string body) {
            var emailObjectView = modelEmailRule.ObjectViews.AddNode<IModelEmailObjectView>();
            emailObjectView.ObjectView = emailObjectView.Application.BOModel.GetClass(typeof(Product)).DefaultDetailView;
            emailObjectView.Subject = emailObjectView.ObjectView.ModelClass.FindMember(nameof(Product.ProductName));
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

        private static IModelEmailRecipient EmailRecipient(this IModelEmail modelEmail, IModelEmailRecipientType recipientType) {
            var emailRecipient = modelEmail.Recipients.AddNode<IModelEmailRecipient>();
            emailRecipient.RecipientTypeCriteria = "[Roles][StartsWith([Name], 'Admin')]";
            emailRecipient.RecipientType = recipientType;
            ((ModelNode)emailRecipient).Id = "Admins";
            return emailRecipient;
        }

        private static IModelEmailAddress SetupEmailAddress(this IModelEmail modelEmail) {
            var emailAddress = modelEmail.EmailAddress.AddNode<IModelEmailAddress>();
            emailAddress.Address = "mail@gmail.com";
            return emailAddress;
        }

        private static IModelEmailRecipientType RecipientType(this IModelEmail modelEmail) {
            var recipientType = modelEmail.RecipientTypes.AddNode<IModelEmailRecipientType>();
            recipientType.Type = modelEmail.Application.BOModel.GetClass(typeof(User));
            recipientType.EmailMember = recipientType.Type.FindMember(nameof(User.Email));
            recipientType.Id("User-Email");
            return recipientType;
        }

        private static IModelEmailSmtpClient SmtpClient(this IModelEmail modelEmail, IModelEmailAddress emailAddress) {
            var smtpClient = modelEmail.SmtpClients.AddNode<IModelEmailSmtpClient>();
            smtpClient.From = emailAddress;
            smtpClient.Host = "smtp.gmail.com";
            smtpClient.Port = 587;
            smtpClient.EnableSsl = true;
            smtpClient.UserName=emailAddress;
            smtpClient.DeliveryMethod=SmtpDeliveryMethod.SpecifiedPickupDirectory;
            smtpClient.Password = "password";
            smtpClient.PickupDirectoryLocation = $"{AppDomain.CurrentDomain.ApplicationPath()}\\TestApplication";
            ((ModelNode)smtpClient).Id = "smtp.gmail.com";
            var emailAddressesDep = smtpClient.ReplyTo.AddNode<IModelEmailAddressesDep>();
            emailAddressesDep.EmailAddress=emailAddress;
            
            return smtpClient;
        }
    }
}