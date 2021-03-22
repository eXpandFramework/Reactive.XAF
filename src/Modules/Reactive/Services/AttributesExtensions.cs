using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.XAF.Modules.Reactive.Services {
    public static class AttributesExtensions {
        internal static IObservable<Unit> Attributes(this ApplicationModulesManager manager)
            => manager.ReadOnlyObjectViewAttribute()
                .Merge(manager.WhenCustomizeTypesInfo().CustomAttributes().ToUnit())
                .Merge(manager.InvisibleInAllViewsAttribute())
                .Merge(manager.VisibleInAllViewsAttribute())
                .Merge(manager.XpoAttributes());

        static IObservable<Unit> ReadOnlyObjectViewAttribute(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViews>().SelectMany().OfType<IModelObjectView>()
                .SelectMany(view => view.ModelClass.TypeInfo.FindAttributes<ReadOnlyObjectViewAttribute>()
                    .Where(objectView =>objectView is IModelDetailView&& objectView.ViewType==ViewType.DetailView||objectView is IModelListView&& objectView.ViewType==ViewType.ListView||objectView.ViewType==ViewType.Any)
                    .Execute(objectView => {
                        view.AllowEdit = objectView.AllowEdit;
                        view.AllowDelete = objectView.AllowDelete;
                        view.AllowNew = objectView.AllowNew;
                    }).ToObservable(Scheduler.Immediate))
                .ToUnit();

        static IObservable<Unit> VisibleInAllViewsAttribute(this ApplicationModulesManager manager)
            => manager.WhenCustomizeTypesInfo()
                .SelectMany(t => t.e.TypesInfo.PersistentTypes
                    .SelectMany(info => info.Members.Where(memberInfo => memberInfo.FindAttributes<VisibleInAllViewsAttribute>().Any())))
                .SelectMany(info => new Attribute[] {new VisibleInDetailViewAttribute(true), new VisibleInListViewAttribute(true), new VisibleInLookupListViewAttribute(true)}
                    .ToObservable(ImmediateScheduler.Instance)
                    .Do(info.AddAttribute))
                .ToUnit();

        static IObservable<Unit> InvisibleInAllViewsAttribute(this ApplicationModulesManager manager)
            => manager.WhenCustomizeTypesInfo()
                .SelectMany(t => t.e.TypesInfo.PersistentTypes
                    .SelectMany(info => info.Members.Where(memberInfo => memberInfo.FindAttributes<InvisibleInAllViewsAttribute>().Any())))
                .SelectMany(info => new Attribute[] {
                        new VisibleInDetailViewAttribute(false), new VisibleInListViewAttribute(false),
                        new VisibleInLookupListViewAttribute(false)
                    }
                    .ToObservable(ImmediateScheduler.Instance)
                    .Do(info.AddAttribute))
                .ToUnit();

        static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> CustomAttributes(this IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> source) 
            => source.ConcatIgnored(_ => source
                .SelectMany(t => t.e.TypesInfo.PersistentTypes
                    .SelectMany(info => info.Members.SelectMany(memberInfo => memberInfo.FindAttributes<Attribute>()
                        .OfType<ICustomAttribute>().ToArray().Select(memberInfo.AddCustomAttribute))
                    ).Concat(t.e.TypesInfo.PersistentTypes.SelectMany(typeInfo => typeInfo
                        .FindAttributes<Attribute>().OfType<ICustomAttribute>().ToArray().Select(typeInfo.AddCustomAttribute))))
                .ToUnit());

        static IObservable<Unit> XpoAttributes(this ApplicationModulesManager manager)
            => manager.WhenCustomizeTypesInfo()
                .SelectMany(_ => new[]{"SingleObjectAttribute","PropertyConcatAttribute"})
                .SelectMany(attributeName => {
                    var lastObjectAttributeType = AppDomain.CurrentDomain.GetAssemblyType($"Xpand.Extensions.XAF.Xpo.{attributeName}");
                    return lastObjectAttributeType != null ? (IEnumerable<IMemberInfo>) lastObjectAttributeType
                        .Method("Configure", Flags.StaticAnyVisibility).Call(null) : Enumerable.Empty<IMemberInfo>();
                } )
                .ToUnit();

    }
}
