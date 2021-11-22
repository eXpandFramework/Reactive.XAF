using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Mail;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Email{
	public interface IModelReactiveModulesEmail : IModelReactiveModule{
		IModelEmail Email{ get; }
	}

	public interface IModelEmail : IModelNode{
		IModelRecipientTypes RecipientTypes { get; }
		IModelEmailRules Rules { get; }
		IModelRecipients Recipients { get; }
		IModelSmtpClients SmtpClients { get; }
		IModelEmailAddresses EmailAddress { get; }
		
	}

	[ModelNodesGenerator(typeof(ModelRecipientTypesNodesGenerator))]
	public interface IModelRecipientTypes:IModelNode,IModelList<IModelEmailRecipientType> { }

	public class ModelRecipientTypesNodesGenerator:ModelNodesGeneratorBase {
		protected override void GenerateNodesCore(ModelNode node) { }
	}

	public interface IModelEmailRecipientType:IModelNode {
		[RefreshProperties(RefreshProperties.All)]
		[Required]
		[DataSourceProperty(nameof(RecipientTypes))]
		IModelClass Type { get; set; }
		[DataSourceProperty(nameof(RecipientEmailMembers))][Required]
		IModelMember EmailMember { get; set; }
		
		[Browsable(false)]
		IModelList<IModelClass> RecipientTypes { get; }
		[Browsable(false)]
		IModelList<IModelMember> RecipientEmailMembers { get; }
	}

	[DomainLogic(typeof(IModelEmailRecipientType))]
	public class ModelRecipientTypeLogic {
		public static IModelList<IModelClass> Get_RecipientTypes(IModelEmailRecipientType recipientType) 
			=> recipientType.Application.BOModel.ToCalculatedModelNodeList();
		
		public static IModelList<IModelMember> Get_RecipientEmailMembers(IModelEmailRecipientType recipientType)
			=> recipientType.Type == null ? new CalculatedModelNodeList<IModelMember>()
				: recipientType.Type.AllMembers.Where(member => member.MemberInfo.MemberType == typeof(string)).ToCalculatedModelNodeList();
	}
	public interface IModelEmailAddresses:IModelList<IModelEmailAddress>,IModelNode {
		
	}

	[KeyProperty(nameof(Address))]
	public interface IModelEmailAddress : IModelNode {
		[RuleRegularExpression(null, DefaultContexts.Save, @"^[_a-z0-9-]+(\.[_a-z0-9-]+)*@[a-z0-9-]+(\.[a-z0-9-]+)*(\.[a-z]{2,4})$")]
		string Address { get; set; }
	}

	[DomainLogic(typeof(IModelEmail))]
	public static class ModelEmailLogic {
		public static IObservable<IModelEmail> Email(this IObservable<IModelReactiveModules> source) 
			=> source.Select(modules => modules.Email());

		public static IModelEmail Email(this IModelReactiveModules reactiveModules) 
			=> ((IModelReactiveModulesEmail) reactiveModules).Email;
		internal static IModelEmail ModelEmail(this IModelApplication modelApplication) 
			=> modelApplication.ToReactiveModule<IModelReactiveModulesEmail>().Email;
	}
	
	public interface IModelEmailRules:IModelNode,IModelList<IModelEmailRule> {
		
	}

	public interface IModelEmailRule:IModelNode {
		[Required]
		[DataSourceProperty("Application.BOModel")][RefreshProperties(RefreshProperties.All)]
		IModelClass Type { get; set; }
		IModelEmailObjectViews ObjectViews { get; }
		
		IModelEmailViewRecipients ViewRecipients { get; }
		
	}

	public interface IModelEmailViewRecipients:IModelNode,IModelList<IModelEmailViewRecipient> { }

	public interface IModelEmailViewRecipient:IModelNode {
		[Required][DataSourceProperty(nameof(SmtpClients))]
		IModelEmailSmtpClient SmtpClient { get; set; } 
		[Localizable(true)]
		string Caption { get; set; }
		[Required][DataSourceProperty(nameof(Recipients))]
		IModelEmailRecipient Recipient { get; set; }
		[Required][DataSourceProperty(nameof(ObjectViews))]
		IModelEmailObjectView ObjectView { get; set; }
		[Browsable(false)]
		IModelList<IModelEmailRecipient> Recipients { get; }
		[Browsable(false)]
		IModelList<IModelEmailObjectView> ObjectViews { get; }
		[Browsable(false)]
		IModelList<IModelEmailSmtpClient> SmtpClients { get; }
	}

	[DomainLogic(typeof(IModelEmailViewRecipient))]
	public class ModelViewRecipientLogic {
		public string Get_Caption(IModelEmailViewRecipient recipient) => $"{recipient.ObjectView?.Id()} to {recipient.Recipient?.Id()}";
		
		public IModelList<IModelEmailSmtpClient> Get_SmtpClients(IModelEmailViewRecipient recipient) => recipient.GetParent<IModelEmail>().SmtpClients;
		public IModelList<IModelEmailRecipient> Get_Recipients(IModelEmailViewRecipient recipient) => recipient.GetParent<IModelEmail>().Recipients;
		public IModelList<IModelEmailObjectView> Get_ObjectViews(IModelEmailViewRecipient recipient) => recipient.GetParent<IModelEmailRule>().ObjectViews;
	}

	public interface IModelRecipients:IModelNode,IModelList<IModelEmailRecipient> { }

	public interface IModelEmailRecipient : IModelNode {
		[Required][RefreshProperties(RefreshProperties.All)]
		IModelEmailRecipientType RecipientType { get; set; }
		[CriteriaOptions(nameof(RecipientType)+"."+nameof(IModelEmailRecipientType.Type)+"."+nameof(IModelEmailRecipientType.Type.TypeInfo))]
		[Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" + XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, DevExpress.Utils.ControlConstants.UITypeEditor)]
		string RecipientTypeCriteria { get; set; }
		
	}

	public interface IModelEmailObjectViews:IModelNode,IModelList<IModelEmailObjectView> { }

	public interface IModelEmailObjectView : IModelNode {
		[Required]
		[DataSourceProperty(nameof(ObjectViews))]
		[RefreshProperties(RefreshProperties.All)]
		IModelObjectView ObjectView { get; set; }
		[Required]
		[DataSourceProperty(nameof(StringMembers))]
		[Category("MailMessage")]
		IModelMember Subject { get; set; }
		[DataSourceProperty(nameof(StringMembers))]
		[Category("MailMessage")]
		IModelMember Body { get; set; }
		[DefaultValue(true)]
		bool UniqueSend { get; set; }
		[Browsable(false)]
		IModelList<IModelObjectView> ObjectViews { get; }
		[Browsable(false)]
		IModelList<IModelMember> StringMembers { get; }
		[Browsable(false)]
		IModelList<IModelMember> AllMembers { get; }

	}

	[DomainLogic(typeof(IModelEmailObjectView))]
	public class ModelEmailObjectViewLogic {
		public IModelList<IModelObjectView> Get_ObjectViews(IModelEmailObjectView objectView) 
			=> objectView.Application.Views.OfType<IModelObjectView>()
				.Where(view => view.ModelClass == objectView.GetParent<IModelEmailRule>()?.Type).ToCalculatedModelNodeList();
		
		public IModelList<IModelMember> Get_StringMembers(IModelEmailObjectView objectView) 
			=> objectView.ObjectView != null
				? objectView.ObjectView.MemberViewItems().Select(item => item.ModelMember)
					.Where(item => item.Type == typeof(string))
					.ToCalculatedModelNodeList()
				: Enumerable.Empty<IModelMember>().ToCalculatedModelNodeList();
	}
	public interface IModelSmtpClients:IModelNode,IModelList<IModelEmailSmtpClient> { }
	[ModelDisplayName("SmtpClient")]
	public interface IModelEmailSmtpClient : IModelNode {
		[Category("SmtpClient"), ModelBrowsable(typeof(ModelSmtpClientDeliveryMethodVisibilityCalculator))]
		bool EnableSsl { get; set; }
		[Required(typeof(ModelSmtpClientDeliveryMethodRequiredCalculator)), Category("SmtpClient"),ModelBrowsable(typeof(ModelSmtpClientDeliveryMethodVisibilityCalculator))]
		string Host { get; set; }
		[Required(typeof(ModelSmtpClientDeliveryMethodRequiredCalculator)), Category("Credentials"), ModelBrowsable(typeof(ModelSmtpClientUseDefaultCredentialsVisibilityCalculator))]
		string Password { get; set; }

		[DefaultValue(25), Category("SmtpClient"),
		 ModelBrowsable(typeof(ModelSmtpClientDeliveryMethodVisibilityCalculator))]
		int Port { get; set; }
		[Required][DataSourceProperty(nameof(FromEmails))]
		IModelEmailAddress From { get; set; }
		IModelEmailAddressesDeps ReplyTo { get;  }
		
		[Category("Credentials"), ModelBrowsable(typeof(ModelSmtpClientDeliveryMethodVisibilityCalculator)), DefaultValue(true), RefreshProperties(RefreshProperties.All)]
		bool UseDefaultCredentials { get; set; }
		[Category("Credentials"), ModelBrowsable(typeof(ModelSmtpClientUseDefaultCredentialsVisibilityCalculator)), Required(typeof(ModelSmtpClientDeliveryMethodRequiredCalculator))]
		IModelEmailAddress UserName { get; set; }
		[Category("SmtpClient"),RefreshProperties(RefreshProperties.All)]
		SmtpDeliveryMethod DeliveryMethod { get; set; }
		[Category("SmtpClient")]
		string PickupDirectoryLocation { get; set; }
		[Browsable(false)]
		IModelList<IModelEmailAddress> FromEmails { get; }
	}

	public interface IModelEmailAddressesDeps:IModelList<IModelEmailAddressesDep>,IModelNode {
		
	}

	[ModelDisplayName(nameof(EmailAddress))]
	[KeyProperty(nameof(Email))]
	public interface IModelEmailAddressesDep : IModelNode {
		[Browsable(false)]
		string Email { get; set; }
		[Required][DataSourceProperty(nameof(EmailAddresses))]
		IModelEmailAddress EmailAddress { get; set; }
		[Browsable(false)]
		IModelList<IModelEmailAddress> EmailAddresses { get; }
	}

	[DomainLogic(typeof(IModelEmailAddressesDep))]
	public class ModelEmailAddressesDepLogic {
		public static IModelEmailAddress Get_EmailAddress(IModelEmailAddressesDep addressesDep) => addressesDep.EmailAddresses[addressesDep.Email];

		public static void Set_EmailAddress(IModelEmailAddressesDep addressesDep,IModelEmailAddress address) => addressesDep.Email = address?.Address;
		
		public static IModelList<IModelEmailAddress> Get_EmailAddresses(IModelEmailAddressesDep addressesDep) 
			=> addressesDep.GetParent<IModelEmail>().EmailAddress;
	}

	[DomainLogic(typeof(IModelEmailSmtpClient))]
	public class ModelSmtpClientLogic {
		public static IModelList<IModelEmailAddress> Get_FromEmails(IModelEmailSmtpClient client) => client.GetParent<IModelEmail>().EmailAddress;
		public static string Get_PickupDirectoryLocation(IModelEmailSmtpClient client) => $@"%temp%\{client.Application.Title}";
	}
	
	public class ModelSmtpClientDeliveryMethodVisibilityCalculator:IModelIsVisible{
		public bool IsVisible(IModelNode node, string propertyName) 
			=> ((IModelEmailSmtpClient) node).DeliveryMethod == SmtpDeliveryMethod.Network;
	}

	public class ModelSmtpClientUseDefaultCredentialsVisibilityCalculator : IModelIsVisible {
		public bool IsVisible(IModelNode node, string propertyName) 
			=> !((IModelEmailSmtpClient) node).UseDefaultCredentials && new ModelSmtpClientDeliveryMethodVisibilityCalculator().IsVisible(node, propertyName);
	}
	
	public class ModelSmtpClientDeliveryMethodRequiredCalculator:IModelIsRequired{
		public bool IsRequired(IModelNode node, string propertyName) 
			=> new ModelSmtpClientDeliveryMethodVisibilityCalculator().IsVisible(node, propertyName);
	}

}