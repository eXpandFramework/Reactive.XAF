using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
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
using DevExpress.Xpo;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.EventArgExtensions;
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
using LookupPropertyEditor = DevExpress.ExpressApp.Blazor.Editors.LookupPropertyEditor;

namespace Xpand.XAF.Modules.TenantManager{
    public static class TenantManagerService {
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => {
                var whenModelReady = application.WhenModelChanged().Skip(1).Publish().RefCount();
                return application.WhenStartupView()
                    .Merge(application.WhenModelChanged().FirstAsync().RegisterOrganizationNonSecured(application))
                    .Merge(whenModelReady.MarkupMessage());
            });

        private static IObservable<Unit> AutoLogin(this DialogController controller) {
            var model = controller.Application.Model.TenantManager();
            var autoLogin = model.AutoLogin;
            if (autoLogin) {
                
                var organizations = controller.Application.Organizations();
                if (organizations.Count == 1) {
                    model.StartupViewOrganization.MemberInfo.SetValue(controller.Frame.View.CurrentObject,organizations.First());
                    return Observable.Timer(TimeSpan.FromMilliseconds(100)).ObserveOnContext()
                        .Do(_ => controller.AcceptAction.DoExecute())
                        .TraceTenantManager()
                        .ToUnit();
                }
            }

            return controller.ReturnObservable().ToUnit();
        }
        
        internal static IObservable<TSource> TraceTenantManager<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, TenantManagerModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        private static IObservable<Unit> WhenOrganizationLookupView(this XafApplication application)
            => application.WhenDetailViewCreated().Where(t => t.e.View.Model==application.Model.TenantManager().StartupView)
                .SelectMany(t => t.e.View.GetItems<LookupPropertyEditor>().Where(editor => editor.MemberInfo.MemberType==application.Model.TenantManager().OrganizationType).ToObservable()
                    .SelectMany(editor => editor.GetFieldValue("helper").WhenEvent(nameof(LookupEditorHelper.CustomCreateCollectionSource))
                        .Select(pattern => pattern.EventArgs).Cast<CustomCreateCollectionSourceEventArgs>()
                        .Do(e => {
                            var collectionSourceBase = application.CreateCollectionSource(
                                application.CreateNonSecuredObjectSpace(), editor.MemberInfo.MemberType,
                                editor.View.Id);
                            collectionSourceBase.Criteria[nameof(TenantManagerService)] = CriteriaOperator.Parse(application.Model.TenantManager().Registration);
                            e.CollectionSource = collectionSourceBase;
                        })))
            // => application.WhenFrameCreated().WhenFrame(Nesting.Nested).Where(frame => frame.Context==TemplateContext.LookupControl)
            //     .SelectMany(frame => application.WhenListViewCreating(application.Model.TenantManager().OrganizationType)
            //         .Do(t => {
            //             var modelListView = (IModelListView)application.Model.Views[t.e.ViewID];
            //             var nonSecuredObjectSpace = application.CreateNonSecuredObjectSpace();
            //             t.e.View = application.CreateListView(modelListView, application.CreateCollectionSource(nonSecuredObjectSpace,modelListView.ModelClass.TypeInfo.Type,modelListView.Id),true);
            //         })
            //         .To(frame))
                // .WhenFrame(frame => frame.Application.Model.TenantManager().OrganizationType)
                // .Cast<NestedFrame>()
                // .Where(frame => frame.Application.Model.TenantManager().StartupView==frame.ViewItem.View.Model)
                // .Do(frame => {
                //     
                //     frame.GetController<NewObjectViewController>().NewObjectAction.Active[nameof(TenantManagerService)] = false;
                //     frame.View.AsListView().CollectionSource.Criteria[nameof(TenantManagerService)]= CriteriaOperator.Parse(frame.View.Model.Application.TenantManager().Registration)
                //         ;
                // })
                .FirstAsync()
                // .TraceTenantManager(t => t?.View?.Id)
                .ToUnit();


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
                .MergeIgnored(frame => frame.View.WhenControlsCreated()
                    .ConfigEditorVisibility().AutoLogon(frame)
                    .FirstAsync().ToUnit()
                )
                .ToController<DialogController>().SelectMany(controller => {
                    var dataStoreProvider = application.ObjectSpaceProvider.DataStoreProvider();
                    return controller.Logoff()
                        .Merge(controller.Logon().HideOrganization().SyncUser(dataStoreProvider));
                })
                .Merge(application.WhenOrganizationLookupView());

        static IObservable<Unit> AutoLogon(this IObservable<View> source, Frame frame)
            => source.SelectMany(_ => frame.GetController<DialogController>().AutoLogin());
        
        static IObservable<View> ConfigEditorVisibility(this IObservable<View> source)
            => source.Do(view => {
                var model = view.Model.Application.TenantManager();
                var editor = ((IAppearanceVisibility)view.AsDetailView().GetPropertyEditor(model.StartupViewOrganization.Name));
                editor.GetPropertyValue("EditButtonModel").SetPropertyValue("Visible", false);
                editor.GetPropertyValue("NewButtonModel").SetPropertyValue("Visible", false);
                editor.Visibility = view.ObjectSpace.OrganizationCount(model.Application)>0 ? ViewItemVisibility.Show : ViewItemVisibility.Hide;
                ((IAppearanceVisibility)view.AsDetailView().GetPropertyEditor(model.StartupViewMessage.Name))
                    .Visibility = editor.Visibility == ViewItemVisibility.Show ? ViewItemVisibility.Hide : ViewItemVisibility.Show;
            })
            .TraceTenantManager();

        public static IList<object> Organizations(this XafApplication application) {
            using var objectSpace = application.CreateNonSecuredObjectSpace();
            var model = application.Model.TenantManager();
            return objectSpace.GetObjects(model.Organization.TypeInfo.Type,CriteriaOperator.Parse(model.Registration)).Cast<object>().ToArray();
        }

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
                .TraceTenantManager()
                .ToUnit();
        private static IObservable<Unit> HideOrganizationNavigation(this XafApplication application) 
            => application.WhenFrameCreated(TemplateContext.ApplicationWindow)
                .ToController<ShowNavigationItemController>()
                .SelectMany(controller => controller.WhenCustomGetStartupNavigationItem()
                    .SelectMany(t => t.e.ActionItems.GetItems<ChoiceActionItem>(item => item.Items))
                    .Where(IsOrganizationItem)
                    .Do(item => item.Active[nameof(TenantManagerService)] = false))
                .TraceTenantManager()
                .ToUnit();

        static bool IsOrganizationItem(this ChoiceActionItem choiceActionItem)
            => choiceActionItem.Data is ViewShortcut shortcut && !string.IsNullOrEmpty(shortcut.ViewId) &&
               choiceActionItem.Model.Application.Views[shortcut.ViewId] is IModelObjectView modelObjectView &&
               modelObjectView.ModelClass == choiceActionItem.Model.Application.TenantManager().Organization;
        
        private static IObservable<Unit> SyncUser(this IObservable<SimpleActionExecuteEventArgs> source, IXpoDataStoreProvider managerProvider)
            => source.SelectMany(e => Observable.Using(() => new XPObjectSpaceProvider(managerProvider), managerObjectProvider 
                => e.Action.Application.WhenCommittedDetailed<ISecurityUser>(ObjectModification.All)
                .SelectMany(t => Observable.Using(managerObjectProvider.CreateObjectSpace, managerObjectSpace
                    => t.SyncUser(managerObjectSpace, e).Concat(Observable.Defer(managerObjectSpace.Commit))))));
        
        
        private static IObservable<Unit> SyncUser(this (IObjectSpace objectSpace, (ISecurityUser user, ObjectModification modification)[] details) t,
            IObjectSpace managerObjectSpace, SimpleActionExecuteEventArgs e) 
            => t.details.Select(t2 => {
                    var modelTenantManager = e.Action.Model.Application.TenantManager();
                    var startupOrganization=modelTenantManager.StartupViewOrganization.MemberInfo.GetValue(e.SelectedObjects.Cast<object>().First());
                    var managerUser = managerObjectSpace.GetUser(t2.user.UserName,modelTenantManager);
                    var managerOrganization = ((IObjectSpaceLink)managerUser).ObjectSpace.GetObject(startupOrganization);
                    managerUser.UpdateManagerOwner(t2.modification, modelTenantManager, managerOrganization);
                    managerUser.UpdateManagerOrganizationUsers(t2.modification, modelTenantManager,managerOrganization);
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

        private static void UpdateManagerOwner(this ISecurityUserWithRoles managerUser, ObjectModification objectModification,
            IModelTenantManager modelTenantManager, object applicationOrganization){
            var managerOwner = managerUser.Roles.Cast<IPermissionPolicyRole>().Any(role => role.IsAdministrative)?managerUser:null;
            managerOwner = objectModification is ObjectModification.New or ObjectModification.Updated ? managerOwner : null;
            var currentOwner = modelTenantManager.Owner.MemberInfo.GetValue(applicationOrganization);
            if (currentOwner == managerOwner || currentOwner==null) {
                modelTenantManager.Owner.MemberInfo.SetValue(applicationOrganization, managerOwner);    
            }
        }

        private static IObservable<SimpleActionExecuteEventArgs> Logon(this DialogController controller) 
            => controller.AcceptAction.WhenExecute(e => {
                var managerUser = (ISecurityUser)SecuritySystem.CurrentUser;
                if (((IObjectSpaceLink)managerUser).ObjectSpace.OrganizationCount(e.Action.Model.Application)==0) {
                    e.Action.Application.LogOff();
                    return controller.ReturnObservable().TraceTenantManager(_ => "LofOff").IgnoreElements().To<SimpleActionExecuteEventArgs>();
                }
                e.SetDataStoreProvider();
                controller.Application.InvokeModuleUpdaters();
                var objectSpace = controller.Application.CreateNonSecuredObjectSpace();
                var model = e.Action.Model.Application.TenantManager();
                var applicationUser = objectSpace.GetUser(managerUser.UserName, model);
                applicationUser.UpdateRoles(e.Action.Application,  objectSpace);
                ((IObjectSpaceLink)applicationUser).CommitChanges();
                ((SecurityStrategyBase)SecuritySystem.Instance).Logon(applicationUser);
                return e.ReturnObservable().TakeUntil(e.Action.Application.WhenLoggedOff().Do(_ => objectSpace.Dispose()));
            });

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

        private static void SetDataStoreProvider(this SimpleActionExecuteEventArgs e) {
            var objectSpaceProvider = e.Action.Application.ObjectSpaceProviders.OfType<XPObjectSpaceProvider>().First();
            var startupObject = e.SelectedObjects.Cast<object>().First();
            var modelTenantManager = e.Action.Model.Application.TenantManager();
            var organization = modelTenantManager.StartupViewOrganization.MemberInfo.GetValue(startupObject);
            objectSpaceProvider.SetDataStoreProvider(new ConnectionStringDataStoreProvider(e.Action.Application.ConnectionString(organization)));
        }

        private static readonly Subject<GenericEventArgs<(XafApplication application, object organization, string connectionString)>> CustomizeConnectionStringSubject = new();
        
        private static bool Owns<T>(this T currentUser, IObjectSpace objectSpace, XafApplication application) where T : ISecurityUser 
            => ((IObjectSpaceLink)currentUser).ObjectSpace.GetObjects(application.Model.TenantManager().OrganizationType,
                CriteriaOperator.Parse($"{application.Model.TenantManager().Owner.Name}.{nameof(currentUser.UserName)}=?", currentUser.UserName))
                .Cast<object>().Select(o => application.ConnectionString(o))
                .Any(connectionString => objectSpace.Connection().ConnectionString == connectionString);

        [UsedImplicitly]
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
