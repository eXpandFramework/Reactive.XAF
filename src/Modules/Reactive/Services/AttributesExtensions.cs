using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;

namespace Xpand.XAF.Modules.Reactive.Services {
    public static class AttributesExtensions {
        internal static IObservable<Unit> Attributes(this ApplicationModulesManager manager)
            => manager.XpoAttributes()
                .Merge(manager.WhenCustomizeTypesInfo()
                    .InvisibleInAllViewsAttribute()
                    .InvisibleInAllListViewsAttribute()
                    .CustomAttributes()
                    .MapTypeMembersAttributes()
                    .VisibleInAllViewsAttribute()
                    .ToUnit())
                .Merge(manager.ReadOnlyCollection())
                .Merge(manager.ReadOnlyProperty())
                .Merge(manager.LookupPropertyAttribute())
                .Merge(manager.ReadOnlyObjectViewAttribute())
        ;
        static IObservable<Unit> ReadOnlyProperty(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelBOModelClassMembers>()
                .SelectMany(members => members.SelectMany(member => member.MemberInfo.FindAttributes<ReadOnlyPropertyAttribute>()
                    .Do(attribute => {
                        member.AllowClear = attribute.AllowClear;
                        member.AllowEdit = false;
                    })))
                .ToUnit();
        
        static IObservable<Unit> ReadOnlyCollection(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenFrame().WhenFrame(ViewType.DetailView)
                .SelectMany(frame => frame.View.AsDetailView().NestedListViews()
                    .SelectMany(editor => editor.MemberInfo.FindAttributes<ReadOnlyCollectionAttribute>()
                        .Select(attribute => (attribute, editor))
                        .Do(t => {
                            var nestedFrameView = t.editor.Frame.View;
                            nestedFrameView.AllowEdit[nameof(ReadOnlyCollectionAttribute)] = t.attribute.AllowEdit;
                            nestedFrameView.AllowDelete[nameof(ReadOnlyCollectionAttribute)] = t.attribute.AllowDelete;
                            nestedFrameView.AllowNew[nameof(ReadOnlyCollectionAttribute)] = t.attribute.AllowNew;
                            t.editor.Frame.GetController<ListViewProcessCurrentObjectController>()
                                    .ProcessCurrentObjectAction.Active[nameof(ReadOnlyCollectionAttribute)] =
                                !t.attribute.DisableListViewProcess;
                        }))))
                .ToUnit();

        static IObservable<Unit> XpoAttributes(this ApplicationModulesManager manager)
            => manager.WhenCustomizeTypesInfo().Take(1).Select(t => t.e.TypesInfo)
                .Do(typesInfo => AppDomain.CurrentDomain.GetAssemblyType("Xpand.Extensions.XAF.Xpo.XpoExtensions")
                    ?.Method("CustomizeTypesInfo",Flags.StaticAnyVisibility).Call(null,typesInfo))
                .ToUnit();

        
        static IObservable<Unit> ReadOnlyObjectViewAttribute(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViews>()
                .Select(views => views)
                .SelectMany().OfType<IModelObjectView>()
                .SelectMany(view => view.ModelClass.TypeInfo.FindAttributes<ReadOnlyObjectViewAttribute>()
                    .Where(objectView => objectView is IModelDetailView && objectView.ViewType == ViewType.DetailView ||
                                         objectView is IModelListView && objectView.ViewType == ViewType.ListView ||
                                         objectView.ViewType == ViewType.Any).ToArray()
                    .Execute(attribute => {
                        view.AllowEdit = attribute.AllowEdit;
                        view.AllowDelete = attribute.AllowDelete;
                        view.AllowNew = attribute.AllowNew;
                        var modelHiddenActions = ((IModelViewHiddenActions) view).HiddenActions;
                        if (!view.AllowEdit) {
                            modelHiddenActions.AddNode<IModelActionLink>("Save", true);
                        }
                        if (attribute.DisableListViewProcess && view is IModelListView) {
                            modelHiddenActions.AddNode<IModelActionLink>(ListViewProcessCurrentObjectController.ListViewShowObjectActionId, true);
                        }
                    }).ToObservable(Transform.ImmediateScheduler))
                .ToUnit()
                .Merge(manager.WhenCustomizeTypesInfo()
                    .SelectMany(t => t.e.TypesInfo.Members<ReadOnlyObjectViewAttribute>().ToObservable()
                        .Where(t1 => t1.info.IsList)
                        .GroupBy(t2 => t2.info.ListElementTypeInfo).Select(tuples => tuples.Key)
                        .Do(info => ((TypeInfo) info).AddAttribute(new ReadOnlyObjectViewAttribute()))
                        .ToUnit()));

        static IObservable<Unit> VisibleInAllViewsAttribute(this IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> source)
            => source.ConcatIgnored(t => t.e.TypesInfo.Members<VisibleInAllViewsAttribute>().ToArray().ToObservable()
                    .SelectMany(t1 => new Attribute[] { new VisibleInDetailViewAttribute(true), new VisibleInListViewAttribute(true), new VisibleInLookupListViewAttribute(true) }
                        .Execute(attribute => t1.info.AddAttribute(attribute))))
                .ToUnit();

        static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> InvisibleInAllViewsAttribute(this IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> source)
            => source.ConcatIgnored(t => t.e.TypesInfo.Members<InvisibleInAllViewsAttribute>().ToArray().ReturnObservable()
                .SelectMany(attributes => attributes.AddVisibleViewAttributes()
                    .Concat(attributes.Distinct(t1 => t1.info).ToArray().AddAppearanceAttributes())));

        private static IEnumerable<Attribute> AddVisibleViewAttributes(this (InvisibleInAllViewsAttribute attribute, IMemberInfo info)[] source) 
            => source.Where(t => t.attribute.Layer == OperationLayer.Model)
                .SelectMany(t => new Attribute[] {
                    new VisibleInDetailViewAttribute(false), new VisibleInListViewAttribute(false),
                    new VisibleInLookupListViewAttribute(false)
                }.Execute(attribute => t.info.AddAttribute(attribute)));
        
        private static IEnumerable<Attribute> AddAppearanceAttributes(this (InvisibleInAllViewsAttribute attribute, IMemberInfo info)[] source) 
            => source.Where(t => t.attribute.Layer == OperationLayer.Appearance)
                .SelectMany(t => new Attribute[] {
                    new AppearanceAttribute($"Hide {t.info}",AppearanceItemType.ViewItem, "1=1") {
                        TargetItems = t.info.Name,Visibility = ViewItemVisibility.ShowEmptySpace
                    }
                }.Execute(attribute => t.info.Owner.AddAttribute(attribute)));

        static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> InvisibleInAllListViewsAttribute(this IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> source)
            => source.ConcatIgnored(t => t.e.TypesInfo.Members<InvisibleInAllListViewsAttribute>().ToArray().ToObservable()
                    .SelectMany(t1 => new Attribute[] {
                        new VisibleInListViewAttribute(false),
                        new VisibleInLookupListViewAttribute(false)
                    }.Execute(attribute => t1.info.AddAttribute(attribute))));

        static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> MapTypeMembersAttributes(this IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> source)
            => source.ConcatIgnored(t => t.e.TypesInfo.PersistentTypes.ToNowObservable()
                .SelectMany(info => info.FindAttributes<MapTypeMembersAttribute>()
                .SelectMany(attribute => attribute.Source.ToTypeInfo().OwnMembers)
                .WhereDefault(memberInfo => info.FindMember(memberInfo.Name))
                .Execute(memberInfo => info.CreateMember(memberInfo.Name, memberInfo.MemberType)).IgnoreElements()
                .ToArray().Finally(() => XafTypesInfo.Instance.RefreshInfo(info))
                .ToNowObservable()));
        static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> CustomAttributes(this IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> source) 
            => source.ConcatIgnored(t => t.e.TypesInfo.PersistentTypes
                .SelectMany(info => info.Members.SelectMany(memberInfo => memberInfo.FindAttributes<Attribute>()
                    .OfType<ICustomAttribute>().ToArray().Select(memberInfo.AddCustomAttribute))
                ).Concat(t.e.TypesInfo.PersistentTypes.SelectMany(typeInfo => typeInfo
                    .FindAttributes<Attribute>().OfType<ICustomAttribute>().ToArray().Select(typeInfo.AddCustomAttribute)))
                .ToObservable(Scheduler.Immediate)
                );

        

    }
}
