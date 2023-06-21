using System;
using System.Collections;
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
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;

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
                .Merge(manager.ListViewShowFooterCollection())
                .Merge(manager.HiddenActions())
                .Merge(manager.ReadOnlyCollection())
                .Merge(manager.ReadOnlyProperty())
                .Merge(manager.LookupPropertyAttribute())
                .Merge(manager.LinkUnlinkPropertyAttribute())
                .Merge(manager.ReadOnlyObjectViewAttribute());

        static IObservable<Unit> LinkUnlinkPropertyAttribute(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenFrame(typeof(object),ViewType.DetailView)
                .SelectMany(frame => frame.View.ObjectTypeInfo.AttributedMembers<LinkUnlinkPropertyAttribute>().ToNowObservable()
                    .SelectMany(t => frame.View.AsDetailView().NestedListViews(t.memberInfo.ListElementType)
                        .Select(editor => editor.Frame.GetController<LinkUnlinkController>())
                        .SelectMany(controller => controller.LinkAction.WhenExecuteCompleted()
                            .SelectMany(e => e.PopupWindowViewSelectedObjects.Cast<object>())
                            .Do(value => {
                                var memberInfo = frame.View.ObjectTypeInfo.FindMember(t.attribute.PropertyName);
                                if (memberInfo.ListElementType != t.memberInfo.ListElementType) {
                                    var target = frame.View.ObjectSpace.CreateObject(memberInfo.ListElementType);
                                    memberInfo.AssociatedMemberInfo.SetValue(target,frame.View.CurrentObject);
                                    memberInfo.ListElementTypeInfo.Members.Single(info =>info.IsPublic&& info.MemberType==t.memberInfo.ListElementType)
                                        .SetValue(target,value);
                                    value = target;
                                }
                                ((IList)memberInfo.GetValue(frame.View.CurrentObject)).Add(value);
                            })
                            .MergeToUnit(controller.UnlinkAction.WhenExecuteCompleted()
                                .SelectMany(e => e.SelectedObjects.Cast<object>())
                                .Do(value => ((IList)frame.View.ObjectTypeInfo.FindMember(t.attribute.PropertyName)
                                    .GetValue(frame.View.CurrentObject)).Remove(value)))))))
                .ToUnit();
                
        static IObservable<Unit> ListViewShowFooterCollection(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViews>()
                .SelectMany(views => views.OfType<IModelListView>()).Where(view =>
                    view.ModelClass.TypeInfo.FindAttributes<ListViewShowFooterAttribute>(true).Any())
                .Do(view => view.IsFooterVisible = true)
                .SelectMany(view => view.Columns.SelectMany(column =>
                    column.ModelMember.MemberInfo.FindAttributes<ColumnSummaryAttribute>(true)
                        .Do(attribute => column.Summary.AddNode<IModelColumnSummaryItem>().SummaryType = attribute.SummaryType)))
                .ToUnit();
        
        static IObservable<Unit> HiddenActions(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViews>()
                .SelectMany(views =>views.OfType<IModelObjectView>()
                    .SelectMany(view => view.ModelClass.TypeInfo.Attributed<HiddenActionAttribute>()
                        .SelectMany(t => t.attribute.Actions).Do(action => ((IModelViewHiddenActions)view).HiddenActions.AddNode<IModelActionLink>(action))
                        .ToUnit()
                        .Concat(view.ModelClass.TypeInfo.AttributedMembers<HiddenActionAttribute>()
                            .SelectMany(t => views.OfType<IModelListView>().Where(listView => listView.ModelClass.TypeInfo.Type==t.memberInfo.ListElementType)
                                .Where(listView => listView.Id==$"{t.memberInfo.Owner.Name}_{t.memberInfo.Name}_ListView").Distinct()
                                .SelectMany(listView => t.attribute.Actions.Where(action => ((IModelViewHiddenActions)listView).HiddenActions[action]==null)
                                    .Do(action => ((IModelViewHiddenActions)listView).HiddenActions.AddNode<IModelActionLink>(action))))
                            .ToUnit())))
                .ToUnit();
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
            => manager.WhenCustomizeTypesInfo().Take(1).Select(e => e.TypesInfo)
                .Do(typesInfo => AppDomain.CurrentDomain.GetAssemblyType("Xpand.Extensions.XAF.Xpo.XpoExtensions")
                    ?.Method("CustomizeTypesInfo",Flags.StaticAnyVisibility).Call(null,typesInfo))
                .ToUnit();

        
        static IObservable<Unit> ReadOnlyObjectViewAttribute(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViews>().SelectMany().OfType<IModelObjectView>()
                .SelectMany(view => view.ModelClass.TypeInfo.FindAttributes<ReadOnlyObjectViewAttribute>()
                    .Where(attribute => view is IModelDetailView && attribute.ViewType == ViewType.DetailView ||
                                         view is IModelListView && attribute.ViewType == ViewType.ListView ||
                                         attribute.ViewType == ViewType.Any).ToArray()
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
                    .SelectMany(e => e.TypesInfo.Members<ReadOnlyObjectViewAttribute>().ToObservable()
                        .Where(t1 => t1.info.IsList)
                        .GroupBy(t2 => t2.info.ListElementTypeInfo).Select(tuples => tuples.Key)
                        .Do(info => ((TypeInfo) info).AddAttribute(new ReadOnlyObjectViewAttribute()))
                        .ToUnit()));

        
        static IObservable<Unit> VisibleInAllViewsAttribute(this IObservable<CustomizeTypesInfoEventArgs> source)
            => source.ConcatIgnored(e => e.TypesInfo.Members<VisibleInAllViewsAttribute>().ToArray().ToObservable()
                    .SelectMany(t1 => new Attribute[] { new VisibleInDetailViewAttribute(true), new VisibleInListViewAttribute(true), new VisibleInLookupListViewAttribute(true) }
                        .Execute(attribute => t1.info.AddAttribute(attribute))))
                .ToUnit();

        static IObservable<CustomizeTypesInfoEventArgs> InvisibleInAllViewsAttribute(this IObservable<CustomizeTypesInfoEventArgs> source)
            => source.ConcatIgnored(e => e.TypesInfo.Members<InvisibleInAllViewsAttribute>().ToArray().Observe()
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

        static IObservable<CustomizeTypesInfoEventArgs> InvisibleInAllListViewsAttribute(this IObservable<CustomizeTypesInfoEventArgs> source)
            => source.ConcatIgnored(e => e.TypesInfo.Members<InvisibleInAllListViewsAttribute>().ToArray().ToObservable()
                    .SelectMany(t1 => new Attribute[] {
                        new VisibleInListViewAttribute(false),
                        new VisibleInLookupListViewAttribute(false)
                    }.Execute(attribute => t1.info.AddAttribute(attribute))));

        static IObservable<CustomizeTypesInfoEventArgs> MapTypeMembersAttributes(this IObservable<CustomizeTypesInfoEventArgs> source)
            => source.ConcatIgnored(e => e.TypesInfo.PersistentTypes.ToNowObservable()
                .SelectMany(info => info.FindAttributes<MapTypeMembersAttribute>()
                .SelectMany(attribute => attribute.Source.ToTypeInfo().OwnMembers)
                .WhereDefault(memberInfo => info.FindMember(memberInfo.Name))
                .Execute(memberInfo => info.CreateMember(memberInfo.Name, memberInfo.MemberType)).IgnoreElements()
                .ToArray().Finally(() => XafTypesInfo.Instance.RefreshInfo(info))
                .ToNowObservable()));
        
        static IObservable<CustomizeTypesInfoEventArgs> CustomAttributes(this IObservable<CustomizeTypesInfoEventArgs> source) 
            => source.ConcatIgnored(e => e.TypesInfo.PersistentTypes
                .SelectMany(info => info.Members.SelectMany(memberInfo => memberInfo.FindAttributes<Attribute>()
                    .OfType<ICustomAttribute>().ToArray().Select(memberInfo.AddCustomAttribute))
                ).Concat(e.TypesInfo.PersistentTypes.SelectMany(typeInfo => typeInfo
                    .FindAttributes<Attribute>().OfType<ICustomAttribute>().ToArray().Select(typeInfo.AddCustomAttribute)))
                .ToObservable(Scheduler.Immediate)
                );

        

    }
}
