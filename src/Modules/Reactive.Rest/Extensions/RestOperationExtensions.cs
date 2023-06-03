using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Data;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Rest.Extensions {
    internal static class RestOperationExtensions {
        public static IObservable<ApplicationModulesManager> ConfigureRestOperationTypes(this ApplicationModulesManager manager) 
            => manager.Observe(modulesManager => modulesManager.WhenCustomizeTypesInfo().Select(t => t.e.TypesInfo)
                .SelectMany(typesInfo => typesInfo.KeyDefaultAttributes()));

        public static IObservable<object> Commit(this IObjectSpace objectSpace, object o,ICredentialBearer bearer) 
            => objectSpace.IsNewObject(o) ? o.Create(bearer) : objectSpace.IsDeletedObject(o) ? o.Delete(bearer) : objectSpace.CommitingRestCall(o, o.Update(bearer),bearer);

        public static IObservable<object> Get(this IObjectSpace objectSpace, Type type,ICredentialBearer bearer)
            => Operation.Get.RestOperation(type.ToTypeInfo())
                .SelectMany(attribute => attribute.Send(objectSpace.CreateObject(type),bearer).Link(objectSpace));

        public static IObservable<ApplicationModulesManager> RestOperationAction(this IObservable<ApplicationModulesManager> source)
            => source.MergeIgnored(manager => manager.ExportedTypes().ToTypeInfo().OperationActionTypes().ToObservable()
                .SelectMany(t => manager.RegisterViewSimpleAction(t.attribute.ActionId, action => {
                        action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
                        action.TargetObjectType = t.info.Type;
                    })
                    .WhenExecute()
                    .SelectMany(e => e.SelectedObjects.Cast<object>().SendAction(t.attribute,e.Action.AsSimpleAction()))));

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


        static IObservable<Unit> SendAction(this IEnumerable<object> source, RestActionOperationAttribute attribute,SimpleAction action) 
            => source.ToObservable().SelectMany(instance => attribute.Send(instance,action.Application
                    .GetCurrentUser<ICredentialBearer>())).ToUnit();

        private static IObservable<RestOperationAttribute> RestOperation(this Operation operation,IBaseInfo info) 
            => info.FindAttributes<RestOperationAttribute>().Where(attribute => attribute.Operation==operation).ToObservable();

        private static IObservable<object> CommitingRestCall(this IObjectSpace objectSpace,object o, IObservable<object> crudSource,ICredentialBearer bearer) 
            => o.GetTypeInfo().Members.Where(info => info.MemberType == typeof(bool) && info.FindAttributes<RestOperationAttribute>().Any())
                .ToObservable(Scheduler.Immediate)
                .SelectMany(info => info.FindAttributes<RestOperationAttribute>()).Where(attribute => {
                    var isObjectFitForCriteria = objectSpace.IsObjectFitForCriteria(o, CriteriaOperator.Parse(attribute.Criteria));
                    return isObjectFitForCriteria.HasValue && isObjectFitForCriteria.Value;
                })
                .SelectMany(attribute => attribute.Send(o, bearer))
                .Concat(Observable.Defer(() => crudSource));
        
        static IObservable<object> Create(this object instance, ICredentialBearer bearer)
            => Operation.Create.RestOperation(instance.GetTypeInfo())
                .SelectMany(attribute => attribute.Send(instance, bearer))
                .InvalidateCache(instance, bearer);

        static IObservable<object> Update(this object instance, ICredentialBearer bearer)
            => Operation.Update.RestOperation(instance.GetTypeInfo())
                .SelectMany(attribute => attribute.Send(instance, bearer))
                .InvalidateCache(instance,bearer);

        static IObservable<T> InvalidateCache<T>(this IObservable<T> source, object instance, ICredentialBearer bearer)
            => source.Merge(Operation.Get.RestOperation(instance.GetTypeInfo())
                .Do(attribute => RestService.CacheStorage.TryRemove($"{bearer.BaseAddress}{attribute.RequestUrl(instance)}", out _))
                .Cast<T>().IgnoreElements());

        static IObservable<object> Delete(this object instance,ICredentialBearer bearer) 
            => Operation.Delete.RestOperation(instance.GetTypeInfo())
                .SelectMany(attribute => attribute.Send(instance,bearer))
                .InvalidateCache(instance,bearer);

    }
}