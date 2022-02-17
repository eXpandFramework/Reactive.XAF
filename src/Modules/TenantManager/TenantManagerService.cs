using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Blazor.Editors;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.TenantManager{
    public static class TenantManagerService {
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => {
                var whenModelReady = application.WhenModelChanged().Skip(1).Publish().RefCount();
                return application.WhenStartupView()
                    .Merge(whenModelReady.RegisterOrganizationNonSecured(application))
                    .Merge(whenModelReady.MarkupMessage());
            });

        private static IObservable<Unit> AutoLogin(this DialogController controller) {
            var model = controller.Application.Model.TenantManager();
            var autoLogin = model.AutoLogin;
            if (autoLogin) {
                var organizations = controller.Frame.View.ObjectSpace.Organizations(controller.Application.Model);
                if (organizations.Count == 1) {
                    model.StartupViewOrganization.MemberInfo.SetValue(controller.Frame.View.CurrentObject,organizations.First());
                    return Observable.Timer(TimeSpan.FromMilliseconds(100)).ObserveOnContext()
                        .Do(_ => controller.AcceptAction.DoExecute()).ToUnit();
                }
            }

            return controller.ReturnObservable().ToUnit();
        }

        private static IObservable<Unit> WhenOrganizationLookupView(this XafApplication application) 
            => application.WhenFrameCreated().WhenFrame(Nesting.Nested).Where(frame => frame.Context==TemplateContext.LookupControl)
                .SelectMany(frame => {
                    frame.GetController<NewObjectViewController>().NewObjectAction.Active[nameof(TenantManagerService)] = false;
                    return frame.WhenViewChanged()
                        .Do(_ => frame.View.AsListView().CollectionSource.Criteria[nameof(TenantManagerService)] =
                            frame.View.ObjectSpace.ParseCriteria(frame.View.Model.Application.TenantManager().Registration));
                })
                .FirstAsync()
                .ToUnit();


        private static IObservable<Unit> MarkupMessage(this IObservable<IModelApplication> source)
            => source.SelectMany(application => {
                var model = application.TenantManager();
                return model.StartupView.Items.OfType<IModelPropertyEditor>().Where(item =>
                        item.ModelMember.MemberInfo == model.StartupViewMessage.MemberInfo).ToNowObservable()
                    .Do(editor => editor.PropertyEditorType = typeof(MarkupContentPropertyEditor));
            }).ToUnit();
        
        private static IObservable<Unit> RegisterOrganizationNonSecured(this IObservable<IModelApplication> source,XafApplication application) 
            => source.Do(model => application.AddNonSecuredType(model.TenantManager().Organization.TypeInfo.Type)).ToUnit();

        private static IObservable<Unit> WhenStartupView(this XafApplication application) 
            => application.WhenStartupFrame()
                .MergeIgnored(frame => frame.View.WhenControlsCreated()
                    .ConfigEditorVisibility().AutoLogon(frame)
                    .FirstAsync().ToUnit()
                    .Merge(application.WhenOrganizationLookupView())
                )
                .ToController<DialogController>().SelectMany(controller => controller.Logoff()
                    .Merge(controller.Logon().HideOrganization().SyncUser(application.ObjectSpaceProvider)));

        static IObservable<Unit> AutoLogon(this IObservable<View> source, Frame frame)
            => source.SelectMany(_ => frame.GetController<DialogController>().AutoLogin());
        
        static IObservable<View> ConfigEditorVisibility(this IObservable<View> source)
            => source.Do(view => {
                var model = view.Model.Application.TenantManager();
                var editor = ((IAppearanceVisibility)view.AsDetailView().GetPropertyEditor(model.StartupViewOrganization.Name));
                editor.GetPropertyValue("EditButtonModel").SetPropertyValue("Visible", false);
                editor.Visibility = view.ObjectSpace.OrganizationCount(model.Application)>0 ? ViewItemVisibility.Show : ViewItemVisibility.Hide;
                ((IAppearanceVisibility)view.AsDetailView().GetPropertyEditor(model.StartupViewMessage.Name))
                    .Visibility = editor.Visibility == ViewItemVisibility.Show ? ViewItemVisibility.Hide : ViewItemVisibility.Show;
            });

        public static IList<object> Organizations(this IObjectSpace objectSpace, IModelApplication model)
            => objectSpace.GetObjects(model.TenantManager().Organization.TypeInfo.Type,
                objectSpace.ParseCriteria(model.TenantManager().Registration)).Cast<object>().ToList();
        
        public static int OrganizationCount(this IObjectSpace objectSpace,IModelApplication model) 
            => objectSpace.GetObjectsCount(model.TenantManager().Organization.TypeInfo.Type, objectSpace.ParseCriteria(model.TenantManager().Registration));

        private static IObservable<Frame> WhenStartupFrame(this XafApplication application) 
            => application.WhenFrameViewChanged()
                .WhenFrame(frame => frame.Application.Model.TenantManager().StartupView?.ModelClass.TypeInfo.Type);

        
        private static IObservable<SimpleActionExecuteEventArgs> HideOrganization(this IObservable<SimpleActionExecuteEventArgs> source)
            => source.MergeIgnored(e => e.Action.Application.HideOrganizationNavigation()
                .Merge(e.Action.Application.HideUserOrganizations()));

        private static IObservable<Unit> HideUserOrganizations(this XafApplication application)
            => application.WhenViewOnFrame(SecuritySystem.UserType,ViewType.DetailView)
                .SelectMany(frame => frame.View.AsDetailView().NestedListViews(frame.Application.Model.TenantManager().OrganizationType).Cast<IAppearanceVisibility>()
                    .Do(editor => editor.Visibility=ViewItemVisibility.Hide))
                .ToUnit();
        private static IObservable<Unit> HideOrganizationNavigation(this XafApplication application) 
            => application.WhenFrameCreated(TemplateContext.ApplicationWindow)
                .ToController<ShowNavigationItemController>()
                .SelectMany(controller => controller.WhenCustomGetStartupNavigationItem()
                    .SelectMany(t => t.e.ActionItems.GetItems<ChoiceActionItem>(item => item.Items))
                    .Where(IsOrganizationItem)
                    .Do(item => item.Active[nameof(TenantManagerService)] = false))
                .ToUnit();

        static bool IsOrganizationItem(this ChoiceActionItem choiceActionItem)
            => choiceActionItem.Data is ViewShortcut shortcut && !string.IsNullOrEmpty(shortcut.ViewId) &&
               choiceActionItem.Model.Application.Views[shortcut.ViewId] is IModelObjectView modelObjectView &&
               modelObjectView.ModelClass == choiceActionItem.Model.Application.TenantManager().Organization;
        
        private static IObservable<Unit> SyncUser(this IObservable<SimpleActionExecuteEventArgs> source, IObjectSpaceProvider managerProvider)
            => source.SelectMany(e => e.Action.Application.WhenCommittedDetailed<ISecurityUser>(ObjectModification.All)
                .SelectMany(t => Observable.Using(managerProvider.CreateObjectSpace, managerObjectSpace 
                    => t.SyncUser(managerObjectSpace, e).Concat(Observable.Defer(managerObjectSpace.Commit)))));
        
        private static IObservable<Unit> SyncUser(this (IObjectSpace objectSpace, (ISecurityUser user, ObjectModification modification)[] details) t,
            IObjectSpace managerObjectSpace, SimpleActionExecuteEventArgs e) 
            => t.details.Select(t2 => {
                    var modelTenantManager = e.Action.Model.Application.TenantManager();
                    
                    var startupOrganization=modelTenantManager.StartupViewOrganization.MemberInfo.GetValue(e.SelectedObjects.Cast<object>().First());
                    
                    var managerUser = managerObjectSpace.GetUser(t2.user.UserName,modelTenantManager);
                    var applicationOrganization = ((IObjectSpaceLink)managerUser).ObjectSpace.GetObject(startupOrganization);
                    
                    var securityUserWithRoles = managerUser.Roles.Cast<IPermissionPolicyRole>().Any(role => role.IsAdministrative)?managerUser:null;
                    var userWithRoles = t2.modification is ObjectModification.New or ObjectModification.Updated ? securityUserWithRoles : null;
                    
                    modelTenantManager.Owner.MemberInfo.SetValue(applicationOrganization, userWithRoles);
                    return managerUser;
                })
                .ToNowObservable().ToUnit();

        

        private static IObservable<SimpleActionExecuteEventArgs> Logon(this DialogController controller) 
            => controller.AcceptAction.WhenExecute(e => {
                var managerUser = (ISecurityUser)SecuritySystem.CurrentUser;
                if (((IObjectSpaceLink)managerUser).ObjectSpace.OrganizationCount(e.Action.Model.Application)==0) {
                    e.Action.Application.LogOff();
                    return Observable.Empty<SimpleActionExecuteEventArgs>();
                }
                e.SetDataStoreProvider();
                controller.Application.InvokeModuleUpdaters();
                var objectSpace = controller.Application.CreateNonSecuredObjectSpace();
                var model = e.Action.Model.Application.TenantManager();
                var applicationUser = objectSpace.GetUser(managerUser.UserName,model);
                var roles = ((IList)model.UserRolesMember().GetValue(applicationUser));
                var adminRole = objectSpace.FindRole(model.AdminRoleCriteria);
                if (((ISecurityUser)SecuritySystem.CurrentUser).Owns( objectSpace,model)) {
                    roles.Add(adminRole);
                }
                else {
                    roles.Add(objectSpace.FindRole(model.DefaultRoleCriteria));
                    roles.Remove(adminRole);
                }
                ((IObjectSpaceLink)applicationUser).CommitChanges();

                ((SecurityStrategyBase)SecuritySystem.Instance).Logon(applicationUser);
                return e.ReturnObservable();
            });

        private static IMemberInfo UserRolesMember(this IModelTenantManager model) 
            => model.Owner.MemberInfo.MemberTypeInfo.Members.First(info => info.IsList&&info.IsAssociation&&info.ListElementType==model.RoleType);

        private static void InvokeModuleUpdaters(this XafApplication application) {
            application.SetPropertyValue("IsCompatibilityChecked", false);
            application.CheckCompatibility();
        }

        private static void SetDataStoreProvider(this SimpleActionExecuteEventArgs e) {
            var objectSpaceProvider = e.Action.Application.ObjectSpaceProviders.OfType<XPObjectSpaceProvider>().First();
            var startupObject = e.SelectedObjects.Cast<object>().First();
            var modelTenantManager = e.Action.Model.Application.TenantManager();
            var organization = modelTenantManager.StartupViewOrganization.MemberInfo.GetValue(startupObject);
            var connectionString = $"{modelTenantManager.ConnectionString.MemberInfo.GetValue(organization)}";
            objectSpaceProvider.SetDataStoreProvider(new ConnectionStringDataStoreProvider(connectionString));
        }

        private static bool Owns<T>(this T currentUser, IObjectSpace objectSpace, IModelTenantManager model) where T : ISecurityUser 
            => ((IObjectSpaceLink)currentUser).ObjectSpace.GetObjects(
                model.OrganizationType, CriteriaOperator.Parse($"{model.Owner.Name}.{nameof(currentUser.UserName)}=?", currentUser.UserName))
            .Cast<object>().Select(o => model.ConnectionString.MemberInfo.GetValue(o)).Cast<string>()
            .Any(connectionString => objectSpace.Connection().ConnectionString==connectionString);

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
