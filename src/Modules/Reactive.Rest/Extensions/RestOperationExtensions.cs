using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Data;
using DevExpress.ExpressApp.DC;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Rest.Extensions {
    internal static class RestOperationExtensions {
        public static IObservable<ApplicationModulesManager> ConfigureRestOperationTypes(this ApplicationModulesManager manager) 
            => manager.ReturnObservable(modulesManager => modulesManager.WhenCustomizeTypesInfo().Select(t => t.e.TypesInfo)
                .SelectMany(typesInfo => typesInfo.KeyDefaultAttributes()));

        public static IObservable<object> Commit(this IObjectSpace objectSpace, object o) 
            => objectSpace.IsNewObject(o) ? o.Create() : objectSpace.IsDeletedObject(o) ? o.Delete() : objectSpace.CommitingRestCall(o, o.Update());

        public static IObservable<object> Get(this IObjectSpace objectSpace, Type type)
            => Operation.Get.RestOperation(type.ToTypeInfo())
                .SelectMany(attribute => attribute.Send(type.CreateInstance()).Link(objectSpace));

        public static IObservable<ApplicationModulesManager> RestOperationAction(this IObservable<ApplicationModulesManager> source)
            => source.MergeIgnored(manager => manager.ExportedTypes().ToTypeInfo().OperationActionTypes().ToObservable()
                .SelectMany(t => manager.RegisterViewSimpleAction(t.attribute.ActionId, action => {
                        action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
                        action.TargetObjectType = t.info.Type;
                    })
                    .WhenExecute()
                    .SelectMany(e => t.attribute.SendAction(e.Action.AsSimpleAction()))));

        private static IObservable<Unit> KeyDefaultAttributes(this ITypesInfo typesInfo) 
            => typesInfo.RestOperationTypes()
                .Do(info => {
                    var keyMember = info.FindMember("Id") ?? info.FindMember("Name");
                    if (keyMember != null) {
                        if (!info.FindAttributes<XafDefaultPropertyAttribute>().Any()) {
                            info.AddAttribute(new XafDefaultPropertyAttribute(keyMember.Name));
                        }
                        keyMember.AddAttribute(new KeyAttribute());
                        ((TypeInfo) info).Refresh(true);
                    }
                })
                .ToUnit();

        private static IObservable<ITypeInfo> RestOperationTypes(this ITypesInfo typesInfo) 
            => typesInfo.PersistentTypes.Where(info => info.FindAttributes<RestOperationAttribute>().Any()).ToObservable();


        static IObservable<Unit> SendAction(this RestActionOperationAttribute attribute,SimpleAction action) 
            => action.WhenExecute().SelectMany(t1 => t1.SelectedObjects.Cast<object>().ToObservable().SelectMany(t2 => attribute.Send(t2))).ToUnit();

        private static IObservable<RestOperationAttribute> RestOperation(this Operation operation,IBaseInfo info) 
            => info.FindAttributes<RestOperationAttribute>().Where(attribute => attribute.Operation==operation).ToObservable();

        private static IObservable<object> CommitingRestCall(this  IObjectSpace objectSpace,object o, IObservable<object> crudSource) {
            
            return o.GetTypeInfo().Members.Where(info =>
                    info.MemberType == typeof(bool) && info.FindAttributes<RestOperationAttribute>().Any())
                .ToObservable(Scheduler.Immediate)
                .SelectMany(info => info.FindAttributes<RestOperationAttribute>()).Where(attribute => {
                    var isObjectFitForCriteria =
                        objectSpace.IsObjectFitForCriteria(o, CriteriaOperator.Parse(attribute.Criteria));
                    return isObjectFitForCriteria.HasValue && isObjectFitForCriteria.Value;
                })
                .SelectMany(attribute => attribute.Send(o))
                .Concat(Observable.Defer(() => crudSource));
        }

        static IObservable<object> Create(this object instance) 
            => Operation.Create.RestOperation(instance.GetTypeInfo())
                .SelectMany(attribute => attribute.Send(instance));

        static IObservable<object> Update(this object instance) 
            => Operation.Update.RestOperation(instance.GetTypeInfo())
                .SelectMany(attribute => attribute.Send(instance));

        static IObservable<object> Delete(this object instance) 
            => Operation.Delete.RestOperation(instance.GetTypeInfo())
                .SelectMany(attribute => attribute.Send(instance));

    }
}