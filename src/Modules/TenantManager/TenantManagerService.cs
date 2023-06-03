using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Fasterflect;

using Xpand.Extensions.Blazor;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Blazor;
using Xpand.XAF.Modules.Blazor.Editors;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using LookupPropertyEditor = DevExpress.ExpressApp.Blazor.Editors.LookupPropertyEditor;

namespace Xpand.XAF.Modules.TenantManager{ 
    public static class TenantManagerService {
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenStartupView()
	            .Merge(application.WhenModelChanged().FirstAsync().RegisterOrganizationNonSecured(application))
	            .Merge(application.LogonLastOrganization())
	            .Merge(application.HideOrganization())
                .Merge(application.WhenLoggingOff().SelectMany(_ => application.SaveOrganizationKey()).ToUnit())
	            .Merge(application.WhenModelChanged().Skip(1).MarkupMessage()));

        private static IObservable<Unit> LogonLastOrganization(this XafApplication application) 
	        => application.WhenLoggedOn()
                .TraceTenantManager(_ => "Logon")
		        .SelectMany(_ => application.LastOrganization())
                .TraceTenantManager(o => $"{o} - {SecuritySystem.CurrentUserName}  ")
		        .SelectMany(organization => {
			        var managerObjectProvider = application.ObjectSpaceProvider.DataStoreProvider();
			        application.SetDataStoreProvider(organization);
			        var objectSpace = application.CreateNonSecuredObjectSpace(SecuritySystem.UserType);
			        var currentUserName = SecuritySystem.CurrentUserName;
			        var applicationUser = objectSpace.FindObject(SecuritySystem.UserType,CriteriaOperator.FromLambda<ISecurityUser>(user => user.UserName==currentUserName));
			        if (applicationUser == null) {
                        return Observable.Empty<Unit>();
			        }
			        ((SecurityStrategyBase)SecuritySystem.Instance).Logon(applicationUser);
			        return application.SyncUserWhenChanged(managerObjectProvider,organization);

                }).ToUnit();
        
        internal static IObservable<TSource> TraceTenantManager<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, TenantManagerModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        private static IObservable<Unit> WhenOrganizationLookupView(this XafApplication application)
            => application.WhenDetailViewCreated().Where(t => t.e.View.Model==application.Model.TenantManager().StartupView)
                .SelectMany(t => t.e.View.GetItems<LookupPropertyEditor>().Where(editor => editor.MemberInfo.MemberType==application.Model.TenantManager().OrganizationType).ToObservable()
                    .SelectMany(editor => editor.GetFieldValue("helper").WhenEvent(nameof(LookupEditorHelper.CustomCreateCollectionSource))
                        .Select(pattern => pattern.EventArgs).Cast<CustomCreateCollectionSourceEventArgs>()
                        .Do(e => {
                            var collectionSourceBase = application.CreateCollectionSource(application.CreateNonSecuredObjectSpace(editor.MemberInfo.MemberType), editor.MemberInfo.MemberType, editor.View.Id);
                            collectionSourceBase.Criteria[nameof(TenantManagerService)] = CriteriaOperator.Parse(application.Model.TenantManager().Registration);
                            e.CollectionSource = collectionSourceBase;
                        })))
                .FirstAsync()
                .ToUnit();

        internal static IObservable<Unit> CreateStartupView(this PopupWindowShowAction action)
            => action.WhenCustomizePopupWindowParams()
                .Do(e => {
                    var model = action.Application.Model.TenantManager();
                    var startupType = model.StartupView.ModelClass.TypeInfo.Type;
                    var objectSpace = action.Application.CreateObjectSpace(startupType);
                    e.View = action.Application.CreateDetailView(objectSpace, model.StartupView, true,
                        objectSpace.CreateObject(startupType));
                })
                .ToUnit();

        internal static IList<PopupWindowShowAction> StartupActions(this XafApplication application, IList<PopupWindowShowAction> actions) {
            if (!((ISecurityUserWithRoles)SecuritySystem.CurrentUser).Roles.Cast<IPermissionPolicyRole>().Any(role => role.IsAdministrative)) {
                var userOrganizations = application.UserOrganizations();
                if (userOrganizations.Count > 1||userOrganizations.Count == 0||!application.Model.TenantManager().AutoLogin) {
	                var startupAction = new PopupWindowShowAction(){Application = application};
	                actions.Add(startupAction);
	                startupAction.CreateStartupView().To<object>().Subscribe(application);    
                }
                else if (userOrganizations.Count==1){
	                application.Logon(userOrganizations.First())
                        .Subscribe(application);
                }
            }
            return actions;
        }
        private static IObservable<Unit> MarkupMessage(this IObservable<IModelApplication> source)
            => source.SelectMany(application => {
                var model = application.TenantManager();
                return model.StartupView.Items.OfType<IModelPropertyEditor>().Where(item =>
                        item.ModelMember.MemberInfo == model.StartupViewMessage.MemberInfo).ToNowObservable()
                    .Do(editor => editor.PropertyEditorType = typeof(MarkupContentPropertyEditor));
            }).ToUnit().TraceTenantManager();
        
        private static IObservable<Unit> RegisterOrganizationNonSecured(this IObservable<IModelApplication> source,XafApplication application) 
            => source.Do(model => application.AddNonSecuredType(model.TenantManager().Organization.TypeInfo.Type)).TraceTenantManager().ToUnit();

        private static IObservable<Unit> WhenStartupView(this XafApplication application) 
            => application.WhenStartupFrame()
                .MergeIgnored(frame => frame.View.WhenControlsCreated().ConfigEditorVisibility(application).ToUnit())
                .ToController<DialogController>()
                .SelectMany(controller => controller.Logoff()
                    .Merge(controller.AcceptAction.WhenExecuted(e => {
                        var organization = application.Organization(e.SelectedObjects.Cast<object>().First());
                        return application.Logon(organization).Select(provider => (organization,provider));
                    })
                        .SelectMany(t => application.SyncUserWhenChanged(t.provider, t.organization))
                        .ToUnit()))
                .Merge(application.WhenOrganizationLookupView());

        static IObservable<View> ConfigEditorVisibility(this IObservable<View> source, XafApplication application)
            => source.Do(view => {
                var model = view.Model.Application.TenantManager();
                var editor = ((LookupPropertyEditor)view.AsDetailView().GetPropertyEditor(model.StartupViewOrganization.Name));
                ((DxComboBoxAdapter)editor.Control).ComponentModel.ChildContent = null;
                using var objectSpace = application.CreateNonSecuredObjectSpace(model.Organization.TypeInfo.Type);
                ((IAppearanceVisibility)editor).Visibility = objectSpace.UserOrganizationCount(model.Application) > 0 ? ViewItemVisibility.Show : ViewItemVisibility.Hide;
                ((IAppearanceVisibility)view.AsDetailView().GetPropertyEditor(model.StartupViewMessage.Name))
                    .Visibility = ((IAppearanceVisibility)editor).Visibility == ViewItemVisibility.Show ? ViewItemVisibility.Hide : ViewItemVisibility.Show;
            })
            .TraceTenantManager();

        public static IList<object> UserOrganizations(this XafApplication application) {
	        var model = application.Model.TenantManager();
            using var objectSpace = application.CreateNonSecuredObjectSpace(model.Organization.TypeInfo.Type);
            return objectSpace.GetObjects(model.Organization.TypeInfo.Type,CriteriaOperator.Parse(model.Registration)).Cast<object>().ToArray();
        }

        static int UserOrganizationCount(this IObjectSpace objectSpace,IModelApplication model) 
            => objectSpace.GetObjectsCount(model.TenantManager().Organization.TypeInfo.Type, objectSpace.ParseCriteria(model.TenantManager().Registration));

        private static IObservable<Frame> WhenStartupFrame(this XafApplication application) 
            => application
                .WhenFrame(frame => frame.Application.Model.TenantManager().StartupView?.ModelClass.TypeInfo.Type);
        
        private static IObservable<Unit> HideOrganization(this XafApplication application) 
            => application.HideOrganizationNavigation().Merge(application.HideUserOrganizations());

        private static IObservable<Unit> HideUserOrganizations(this XafApplication application)
            => application.WhenViewOnFrame(SecuritySystem.UserType,ViewType.DetailView)
	            .Where(frame => !frame.Application.IsTenantManager())
                .SelectMany(frame => frame.View.AsDetailView().NestedListViews(frame.Application.Model.TenantManager().OrganizationType).Cast<IAppearanceVisibility>()
                    .Do(editor => editor.Visibility=ViewItemVisibility.Hide))
                .TraceTenantManager()
                .ToUnit();

        public static bool IsTenantManager(this IObjectSpace objectSpace, Type organizationType) => objectSpace.GetObjectsCount(organizationType, null) > 0;

        public static bool IsTenantManager( this XafApplication application,IObjectSpace space=null) {
            var organizationType = application.Model.TenantManager().Organization.TypeInfo.Type;
            if (space==null) {
                using var objectSpace = application.CreateObjectSpace(organizationType);
                return objectSpace.IsTenantManager(organizationType);
            }
            return space.IsTenantManager(organizationType);
        }

        private static IObservable<Unit> HideOrganizationNavigation(this XafApplication application) 
            => application.WhenFrameCreated(TemplateContext.ApplicationWindow)
	            .Where(frame => !frame.Application.IsTenantManager())
                .ToController<ShowNavigationItemController>()
                .SelectMany(controller => controller.ShowNavigationItemAction.Items.GetItems<ChoiceActionItem>(item => item.Items)
	                .Where(IsOrganizationItem).ToNowObservable()
	                .Do(args => args.Active[nameof(TenantManagerService)]=false))
                .TraceTenantManager()
                .ToUnit();

        static bool IsOrganizationItem(this ChoiceActionItem choiceActionItem)
            => choiceActionItem.Data is ViewShortcut shortcut && !string.IsNullOrEmpty(shortcut.ViewId) &&
               choiceActionItem.Model.Application.Views[shortcut.ViewId] is IModelObjectView modelObjectView &&
               modelObjectView.ModelClass == choiceActionItem.Model.Application.TenantManager().Organization;

        private static IObservable<Unit> SyncUserWhenChanged(this XafApplication application,IXpoDataStoreProvider managerProvider,  object organization) 
	        => Observable.Using(() => new XPObjectSpaceProvider(managerProvider), provider => application.SyncWhenUserChanged(provider, organization));
        
        private static IObservable<Unit> SyncWhenUserChanged(this XafApplication application, IObjectSpaceProvider managerObjectProvider,object organization) 
	        => application.WhenCommittedDetailed<ISecurityUser>(ObjectModification.All)
		        .SelectMany(t => Observable.Using(managerObjectProvider.CreateObjectSpace, managerObjectSpace
			        => t.details.SyncUser(managerObjectSpace, application,organization ).Concat(Observable.Defer(managerObjectSpace.Commit))));
        
        private static IObservable<Unit> SyncUser(this (ISecurityUser user, ObjectModification modification)[] source, IObjectSpace managerObjectSpace, XafApplication application,object organization) 
            => source.Select(t => {
                    var modelTenantManager = application.Model.TenantManager();
                    var managerUser = managerObjectSpace.GetUser(t.user.UserName,modelTenantManager);
                    var managerOrganization = ((IObjectSpaceLink)managerUser).ObjectSpace.GetObject(organization);
                    managerUser.UpdateManagerOrganizationUsers(t.modification, modelTenantManager,managerOrganization);
                    if (t.modification == ObjectModification.Deleted) {
                        application.GetService<SingletonItems>().TryRemove(SecuritySystem.CurrentUserName, out _);
                    }
                    return managerUser;
                })
                .ToNowObservable()
                .TraceTenantManager()
                .ToUnit();

        private static void UpdateManagerOrganizationUsers(this ISecurityUserWithRoles managerUser,ObjectModification modification,IModelTenantManager modelTenantManager,object managerOrganization){
            var users = modelTenantManager.Users.MemberInfo.GetValue(managerOrganization);
            switch (users){
                case XPBaseCollection baseCollection:
                    if (modification == ObjectModification.Deleted)
                        baseCollection.BaseRemove(managerUser);
                    else
                        baseCollection.BaseAdd(managerUser);
                    break;
                case IList list:
                    if (modification == ObjectModification.Deleted)
                        list.Remove(managerUser);
                    else
                        list.Add(managerUser);
                    break;
            }
        }
        
        static IObservable<object> LastOrganization(this XafApplication application) 
            => application.UseObjectSpace(space => {
	            var organizationTypeInfo = application.Model.TenantManager().Organization.TypeInfo;
                var lastOrganizationKey = application.LastOrganizationKey();
                return space.GetObjectByKey(organizationTypeInfo.Type, lastOrganizationKey)
                    .Observe().WhenNotDefault();
            }).TraceTenantManager(o => $"{o} - {SecuritySystem.CurrentUserName}  ");

        public static object LastOrganizationKey(this XafApplication application) { 
            application.GetService<SingletonItems>().TryGetValue(SecuritySystem.CurrentUserName,out var value);
            return value;
        }

        internal static IObservable<IXpoDataStoreProvider> Logon(this XafApplication application, object organization) {
	        if (organization == null) {
		        application.LogOff();
		        return Observable.Empty<IXpoDataStoreProvider>();
	        }
            var managerProvider = application.ObjectSpaceProvider.DataStoreProvider();
            application.SetDataStoreProvider(organization);
            application.InvokeModuleUpdaters();
	        var objectSpace = application.CreateNonSecuredObjectSpace(SecuritySystem.UserType);
	        var model = application.Model.TenantManager();
	        var applicationUser = objectSpace.GetUser(SecuritySystem.CurrentUserName, model);
	        applicationUser.UpdateRoles(application, objectSpace);
	        ((IObjectSpaceLink)applicationUser).CommitChanges();
	        ((SecurityStrategyBase)SecuritySystem.Instance).Logon(applicationUser);
            return application.SaveOrganizationKey( organization.GetTypeInfo().KeyMember.GetValue(organization)).To((managerProvider));
        }

        private static IObservable<Unit> SaveOrganizationKey(this XafApplication application, object value=null) {
            var singletonItems = application.GetService<SingletonItems>();
            if (value == null) {
                singletonItems.TryRemove(SecuritySystem.CurrentUserName, out _);
            }
            else if (singletonItems.TryGetValue(SecuritySystem.CurrentUserName,out var id)) {
                singletonItems.TryUpdate(SecuritySystem.CurrentUserName, value, id);
            }
            else {
                singletonItems.TryAdd(SecuritySystem.CurrentUserName, value);
            }
            return Unit.Default.Observe();
        }

        private static void UpdateRoles(this ISecurityUserWithRoles applicationUser,XafApplication application, IObjectSpace objectSpace){
            var model = application.Model.TenantManager();
            var roles = (IList)model.UserRolesMember().GetValue(applicationUser);
            var adminRole = objectSpace.FindRole(model.AdminRoleCriteria);
            var defaultRole = objectSpace.FindRole(model.DefaultRoleCriteria);
            if (((ISecurityUser)SecuritySystem.CurrentUser).Owns(objectSpace, application)){
                roles.Add(adminRole);
                roles.Remove(defaultRole);
            }
            else{
                roles.Add(defaultRole);
                roles.Remove(adminRole);
            }
        }

        private static IMemberInfo UserRolesMember(this IModelTenantManager model) 
            => model.Owner.MemberInfo.MemberTypeInfo.Members.First(info => info.IsList&&info.IsAssociation&&info.ListElementType==model.RoleType);

        private static void InvokeModuleUpdaters(this XafApplication application) {
            application.SetPropertyValue("IsCompatibilityChecked", false);
            application.CheckCompatibility();
        }
        
        private static void SetDataStoreProvider(this XafApplication application,object organization) {
            var provider = application.ManagerDataStoreProvider( organization);
            application.ObjectSpaceProviders.OfType<XPObjectSpaceProvider>().First().SetDataStoreProvider((IXpoDataStoreProvider)provider);
        }

        [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
        static object ManagerDataStoreProvider(this XafApplication application, object organization) 
            => application.GetService<SingletonItems>().GetOrAdd(organization.GetTypeInfo().KeyMember.GetValue(organization),
                    _ => new ConnectionStringDataStoreProvider(application.ConnectionString(organization)));
        
        
        public static IXpoDataStoreProvider ManagerDataStoreProvider(this XafApplication application) {
            application.GetService<SingletonItems>().TryGetValue(application.LastOrganizationKey(),out var value);
            return (IXpoDataStoreProvider)value;
        }

        private static object Organization(this XafApplication application,object startupObject) 
            => application.Model.TenantManager().StartupViewOrganization.MemberInfo.GetValue(startupObject);

        private static readonly Subject<GenericEventArgs<(XafApplication application, object organization, string connectionString)>> CustomizeConnectionStringSubject = new();
        
        private static bool Owns<T>(this T currentUser, IObjectSpace objectSpace, XafApplication application) where T : ISecurityUser 
	        => ((IObjectSpaceLink)currentUser).ObjectSpace.GetObjects(application.Model.TenantManager().OrganizationType,
		        CriteriaOperator.Parse($"{application.Model.TenantManager().Owner.Name}.{nameof(currentUser.UserName)}=?", currentUser.UserName))
	        .Cast<object>().Select(o => application.ConnectionString(o))
	        .Any(connectionString => Regex.IsMatch(connectionString, $@"{objectSpace.Connection().Database}\b", RegexOptions.IgnoreCase));

        
        public static IObservable<Unit> WhenCustomizeConnectionString<T>(this XafApplication application,Func<T,string> connectionString) 
            => CustomizeConnectionStringSubject.AsObservable().Where(e => e.Instance.application == application)
                .Select(e => e.SetInstance(t => (t.application,t.organization,connectionString((T)t.organization))))
                .ToUnit();

        private static string ConnectionString(this XafApplication application,object organization) {
            var args = new GenericEventArgs<(XafApplication application, object organization, string connectionString)>(
                (application, organization, $"{application.Model.TenantManager().ConnectionString.MemberInfo.GetValue(organization)}"));
            CustomizeConnectionStringSubject.OnNext(args);
            return args.Instance.connectionString;
        }

        private static ISecurityUserWithRoles GetUser(this IObjectSpace objectSpace, string userName, IModelTenantManager model){
            var applicationUser = objectSpace.FindObject(SecuritySystem.UserType,CriteriaOperator.FromLambda<ISecurityUser>(user => user.UserName==userName));
            if (applicationUser == null){
                applicationUser = objectSpace.CreateObject(SecuritySystem.UserType);
                applicationUser.SetPropertyValue(nameof(ISecurityUser.UserName), userName);
                ((IList)model.UserRolesMember().GetValue(applicationUser)).Add(objectSpace.FindRole(model.DefaultRoleCriteria));
                objectSpace.CommitChanges();
            }
            return (ISecurityUserWithRoles)applicationUser;
        }

        private static object FindRole(this IObjectSpace objectSpace, string criteria) 
            => objectSpace.FindObject((((SecurityStrategyComplex)SecuritySystem.Instance).RoleType), objectSpace.ParseCriteria(criteria));

        private static IObservable<Unit> Logoff(this DialogController controller) 
            => controller.CancelAction.WhenExecuting()
                .Do(t => t.action.Application.LogOff())
                .ToUnit();
    }

}
