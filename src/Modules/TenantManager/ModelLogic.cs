using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.TenantManager{
	public interface IModelReactiveModulesTenantManager : IModelReactiveModule{
		IModelTenantManager TenantManager{ get; }
	}

	internal static class TenantManagerModelExtensions {
		public static IModelTenantManager TenantManager(this IModelApplication application) 
			=> application.ToReactiveModule<IModelReactiveModulesTenantManager>().TenantManager;
	}

	public interface IModelTenantManager : IModelNode{
		[DataSourceProperty(nameof(StartupViews))]
		[Required][RefreshProperties(RefreshProperties.All)]
		[Category(nameof(StartupView))]
		[ModelReadOnly(typeof(TenantManagerModelReadOnly))]
		IModelDetailView StartupView { get; set; }
		[Required]
		[DataSourceProperty(nameof(StartupViewOrganizations))]
		[Category(nameof(StartupView))]
		[ModelReadOnly(typeof(TenantManagerModelReadOnly))]
		IModelMember StartupViewOrganization { get; set; }
		[Required]
		[DataSourceProperty(nameof(StartupViewStrings))]
		[Category(nameof(StartupView))]
		[ModelReadOnly(typeof(TenantManagerModelReadOnly))]
		IModelMember StartupViewMessage { get; set; }


		[DataSourceProperty(nameof(Organizations))]
		[Required][RefreshProperties(RefreshProperties.All)]
		[Category(nameof(Organization))]
		IModelClass Organization { get; set; }
		[Category(nameof(Organization))]
		[DefaultValue(true)]
		bool AutoLogin { get; set; }
		[Required]
		[DataSourceProperty(nameof(Owners))]
		[Category(nameof(Organization))]
		[ModelReadOnly(typeof(TenantManagerModelReadOnly))]
		[RefreshProperties(RefreshProperties.All)]
		IModelMember Owner { get; set; }
		[Required]
		[DataSourceProperty(nameof(UsersMembers))]
		[Category(nameof(Organization))]
		[ModelReadOnly(typeof(TenantManagerModelReadOnly))]
		[RefreshProperties(RefreshProperties.All)]
		IModelMember Users { get; set; }
		[Required][Category(nameof(Organization))]
		[ModelReadOnly(typeof(TenantManagerModelReadOnly))]
		
		[Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" +
		        XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, DevExpress.Utils.ControlConstants.UITypeEditor)]
		[CriteriaOptions(nameof(OrganizationType))]
		
		string Registration { get; set; }
		[Browsable(false)]
		public Type OrganizationType { get; }
		[Required]
		[DataSourceProperty(nameof(OrganizationStrings))]
		[Category(nameof(Organization))]
		[ModelReadOnly(typeof(TenantManagerModelReadOnly))]
		IModelMember ConnectionString { get; set; }
		
		[Browsable(false)]
		IModelList<IModelMember> Owners { get; }
		[Browsable(false)]
		IModelList<IModelMember> UsersMembers { get; }
		[Browsable(false)]
		IModelList<IModelMember> OrganizationStrings { get; }
		[Browsable(false)]
		IModelList<IModelMember> StartupViewStrings { get; }
		[Browsable(false)]
		IModelList<IModelDetailView> StartupViews { get; }
		[Browsable(false)]
		IModelList<IModelClass> Organizations { get; }
		[Browsable(false)]
		IModelList<IModelMember> StartupViewOrganizations { get; }

		[Category("Organization.NewUsers")]
		[Required][ModelReadOnly(typeof(TenantManagerModelReadOnly))]
		[Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" +
		        XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, DevExpress.Utils.ControlConstants.UITypeEditor)]
		[CriteriaOptions(nameof(RoleType))]
		string DefaultRoleCriteria { get; set; }
		[Category("Organization.NewUsers")]
		[Required][ModelReadOnly(typeof(TenantManagerModelReadOnly))]
		[Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" +
		        XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, DevExpress.Utils.ControlConstants.UITypeEditor)]
		[CriteriaOptions(nameof(RoleType))]
		string AdminRoleCriteria { get; set; }
		
		[Browsable(false)]
		Type RoleType { get; }
	}

	[DomainLogic(typeof(IModelTenantManager))]
	public static class ModelTenantManagerLogic {
		[UsedImplicitly]
		public static Type Get_OrganizationType(this IModelTenantManager manager)
			=> manager.Organization?.TypeInfo.Type;
		
		[UsedImplicitly]
		public static string Get_AdminRoleCriteria(this IModelTenantManager manager)
			=> GetRoleCriteria("Administrators");
		
		[UsedImplicitly]
		public static string Get_DefaultRoleCriteria(this IModelTenantManager manager)
			=> GetRoleCriteria("Default");

		private static string GetRoleCriteria(string roleName) 
			=> CriteriaOperator.FromLambda<IPermissionPolicyRole>(role => role.Name==roleName).ToString();

		[UsedImplicitly]
		public static Type Get_RoleType(this IModelTenantManager manager)
			=> manager.Owner?.MemberInfo.MemberTypeInfo.Members.FirstOrDefault(info =>
				info.IsList && typeof(IPermissionPolicyRole).IsAssignableFrom(info.ListElementType))?.ListElementType;

		[UsedImplicitly]
		public static IModelList<IModelDetailView> Get_StartupViews(this IModelTenantManager manager)
			=> manager.Application.Views.OfType<IModelDetailView>().Where(view => manager.FitsStartup(view.ModelClass))
				.ToCalculatedModelNodeList();

		private static bool FitsStartup(this IModelTenantManager manager, IModelClass modelClass) 
			=> modelClass.AllMembers.Any(member => manager.Organization != null && manager.Organization.TypeInfo.Type.IsAssignableFrom(member.MemberInfo.MemberType));

		[UsedImplicitly]
		public static IModelDetailView Get_StartupView(IModelTenantManager manager)
			=>manager.StartupViews.Count==1? manager.StartupViews.First():null;
		
		[UsedImplicitly]
		public static IModelList<IModelClass> Get_Organizations(this IModelTenantManager manager)
			=> manager.Application.BOModel.Where(c => c.AllMembers.Any(member => typeof(IPermissionPolicyUser).IsAssignableFrom(member.MemberInfo.MemberType)))
				.ToCalculatedModelNodeList();
		
		public static IModelList<IModelMember> Get_StartupViewOrganizations(IModelTenantManager manager) 
			=> manager.StartupView?.ModelClass.AllMembers.Where(member => manager.Organization != null && manager.Organization.TypeInfo.Type.IsAssignableFrom(member.MemberInfo.MemberType))
				.ToCalculatedModelNodeList()??new CalculatedModelNodeList<IModelMember>();
		
		public static IModelMember Get_StartupViewOrganization(IModelTenantManager manager) 
			=> manager.StartupViewOrganizations.Count==1?manager.StartupViewOrganizations.First():null;
		
		public static IModelMember Get_StartupViewMessage(IModelTenantManager manager) 
			=> manager.StartupViewStrings.Count==1?manager.StartupViewStrings.First():null;
		
		[UsedImplicitly]
		public static IModelList<IModelMember> Get_Owners(IModelTenantManager manager) 
			=> manager.Organization?.AllMembers.Where(member => typeof(ISecurityUser).IsAssignableFrom(member.MemberInfo.MemberType))
				.ToCalculatedModelNodeList()??new CalculatedModelNodeList<IModelMember>();
		
		[UsedImplicitly]
		public static IModelList<IModelMember> Get_UsersMembers(IModelTenantManager manager) 
			=> manager.Organization?.AllMembers.Where(member =>member.MemberInfo.IsList&& typeof(ISecurityUser).IsAssignableFrom(member.MemberInfo.ListElementType))
				.ToCalculatedModelNodeList()??new CalculatedModelNodeList<IModelMember>();
		
		[UsedImplicitly]
		public static IModelMember Get_Users(IModelTenantManager manager) 
			=> manager.UsersMembers.FirstOrDefault();
		
			// "[ApplicationUsers][[Oid] = CURRENTUSERID()] Or [Owner.Oid] = CURRENTUSERID()"
		[UsedImplicitly]
		public static string Get_Registration(IModelTenantManager manager) 
			=> $"[{manager.Users?.Name}][[{manager.Owner?.MemberInfo.MemberTypeInfo.KeyMember.Name}]=CURRENTUSERID()] OR [{manager.Owner?.Name}.{manager.Owner?.MemberInfo.MemberTypeInfo.KeyMember.Name}]=CURRENTUSERID()";

		[UsedImplicitly]
		public static IModelMember Get_Owner(IModelTenantManager manager)
			=> manager.Owners.Count==1?manager.Owners.First():null;

		[UsedImplicitly]
		public static IModelList<IModelMember> Get_OrganizationStrings(IModelTenantManager manager) 
			=> manager.Organization?.AllMembers.Where(member => member.MemberInfo.MemberType==typeof(string))
				.ToCalculatedModelNodeList()??new CalculatedModelNodeList<IModelMember>();
		
		[UsedImplicitly]
		public static IModelList<IModelMember> Get_StartupViewStrings(IModelTenantManager manager) 
			=> manager.StartupView?.ModelClass.AllMembers.Where(member => member.MemberInfo.MemberType==typeof(string))
				.ToCalculatedModelNodeList()??new CalculatedModelNodeList<IModelMember>();
		
		[UsedImplicitly]
		public static IModelMember Get_ConnectionString(IModelTenantManager manager) 
			=> manager.OrganizationStrings.Count != 1 ? manager.OrganizationStrings.FirstOrDefault(member => member.Name=="ConnectionString") : manager.OrganizationStrings.First();
	}
	
	public class TenantManagerModelReadOnly:IModelIsReadOnly {
		public bool IsReadOnly(IModelNode node, string propertyName)
			=> new[] {
					nameof(IModelTenantManager.StartupView), nameof(IModelTenantManager.StartupViewOrganization),
					nameof(IModelTenantManager.Owner), nameof(IModelTenantManager.ConnectionString),nameof(IModelTenantManager.DefaultRoleCriteria),
					nameof(IModelTenantManager.AdminRoleCriteria),nameof(IModelTenantManager.Registration),nameof(IModelTenantManager.StartupViewMessage)
				}
				.Contains(propertyName) && (((IModelTenantManager)node).Organization == null);

		public bool IsReadOnly(IModelNode node, IModelNode childNode) => false;
	}


}