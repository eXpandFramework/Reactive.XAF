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
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

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
                .Merge(manager.ColumnSummary())
                .Merge(manager.ColumnSorting())
                .Merge(manager.DisableNewObjectAction())
                .Merge(manager.HiddenActions())
                .Merge(manager.ReadOnlyCollection())
                .Merge(manager.ReadOnlyProperty())
                .Merge(manager.LookupPropertyAttribute())
                .Merge(manager.LinkUnlinkPropertyAttribute())
                .Merge(manager.ReadOnlyObjectViewAttribute())
                .Merge(manager.DetailCollectionAttribute())
            ;

        static IObservable<Unit> LinkUnlinkPropertyAttribute(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenFrame(typeof(object),ViewType.DetailView)
                .SelectMany(frame => frame.View.ObjectTypeInfo.AttributedMembers<LinkUnlinkPropertyAttribute>().ToNowObservable()
                    .SelectMany(t => frame.View.AsDetailView().NestedListViews(t.memberInfo.ListElementType)
                        .SelectMany(editor => editor.WhenLinkUnlinkAction( frame, t)
                            .Merge(editor.WhenNewObjectAction( frame, t)))
                        )))
                .MergeToUnit(manager.WhenCustomizeTypesInfo()
                    .SelectMany(e => e.TypesInfo.Members<LinkUnlinkPropertyAttribute>().Where(t => t.info.Owner.FindMember(t.attribute.PropertyName).ListElementType.IsAbstract)));

        private static IObservable<Unit> WhenNewObjectAction(this ListPropertyEditor editor, Frame frame, (LinkUnlinkPropertyAttribute attribute, IMemberInfo memberInfo) t) 
            => editor.Frame.NewObjectAction().WhenExecuted()
                .SelectMany(e => e.ShowViewParameters.CreatedView.ObjectSpace.WhenCommitted().Take(1).To(e))
                .Do(e => {
                    var memberInfo = frame.View.ObjectTypeInfo.FindMember(t.attribute.PropertyName);
                    ((IList)memberInfo.GetValue(frame.View.CurrentObject))
                        .Add(frame.View.ObjectSpace.GetObject(e.ShowViewParameters.CreatedView.CurrentObject) );
                }).ToUnit();

        private static IObservable<Unit> WhenLinkUnlinkAction(this ListPropertyEditor editor, Frame frame, (LinkUnlinkPropertyAttribute attribute, IMemberInfo memberInfo) t){
            var controller = editor.Frame.GetController<LinkUnlinkController>();
            return controller.LinkAction.WhenExecuteCompleted()
                .SelectMany(e => e.PopupWindowViewSelectedObjects.Cast<object>())
                .Do(value => {
                    var memberInfo = frame.View.ObjectTypeInfo.FindMember(t.attribute.PropertyName);
                    if (!memberInfo.ListElementType.IsAssignableFrom(t.memberInfo.ListElementType)) {
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
                        .GetValue(frame.View.CurrentObject)).Remove(value)));
        }
        
        static IObservable<Unit> DisableNewObjectAction(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenFrameCreated().ToController<NewObjectViewController>()
                .SelectMany(controller => controller.WhenCollectDescendantTypes()
                    .SelectMany(e => e.Types.Where(type => type.Attributes<DisableNewObjectActionAttribute>().Any()).ToArray()
                        .Do(type => e.Types.Remove(type)))))
                .ToUnit();
        
        static IObservable<Unit> ColumnSorting(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelColumns>()
                .SelectMany(columns => columns.SelectMany(column =>
                    column.ModelMember.MemberInfo.FindAttributes<ColumnSortingAttribute>(true)
                        .Do(attribute => {
                            column.SortIndex = attribute.SortIndex;
                            column.SortOrder=attribute.SortOrder;
                        }).ToUnit()))
                .ToUnit();
        
        static IObservable<Unit> ColumnSummary(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelColumns>()
                .SelectMany(views => views.SelectMany(column =>
                    column.ModelMember.MemberInfo.FindAttributes<ColumnSummaryAttribute>(true)
                        .Do(attribute => column.Summary.AddNode<IModelColumnSummaryItem>().SummaryType = attribute.SummaryType)
                        .ToUnit()))
                .MergeToUnit(manager.WhenApplication(application => application.WhenSetupComplete().Where(_ => application.GetPlatform()==Platform.Win)
                    .SelectMany(_ => application.TypesInfo.PersistentTypes.AttributedMembers<ColumnSummaryAttribute>()
                        .GroupBy(t => t.memberInfo.Owner).ToNowObservable()
                        .SelectMany(types => application.WhenFrame(types.Key.Type,ViewType.ListView)
                            .SelectUntilViewClosed(frame => frame.View.WhenControlsCreated(true)
                                .Select(view => view.ToListView().Editor.GridView())
                                .SelectMany(gridView => types.SelectMany(t1 => {
                                    var column = gridView.GetPropertyValue("Columns")
                                        .CallMethod("ColumnByFieldName", t1.memberInfo.BindingName);
                                    return column==null?Enumerable.Empty<object>(): ((IEnumerable)column
                                            .GetPropertyValue("Summary")).Cast<object>()
                                        .Do(item => item.SetPropertyValue("Mode", t1.attribute.SummaryMode));
                                })))
                        ))));
        
        static IObservable<Unit> ListViewShowFooterCollection(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViews>()
                .SelectMany(views => views.OfType<IModelListView>()).Where(view =>
                    view.ModelClass.TypeInfo.FindAttributes<ListViewShowFooterAttribute>(true).Any())
                .Do(view => view.IsFooterVisible = true)
                .ToUnit();
        
        static IObservable<Unit> HiddenActions(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViews>().SelectMany(views =>views.OfType<IModelObjectView>()
                    .SelectMany(view => view.ModelClass.TypeInfo.Attributed<HiddenActionAttribute>()
                        .SelectMany(t => t.attribute.Actions).Distinct().Do(action => ((IModelViewHiddenActions)view).HiddenActions.EnsureNode<IModelActionLink>(action)).ToUnit()
                        .Concat(view.ModelClass.TypeInfo.AttributedMembers<HiddenActionAttribute>()
                            .SelectMany(t => views.OfType<IModelListView>().Where(listView => listView.ModelClass.TypeInfo.Type==t.memberInfo.ListElementType)
                                .Where(listView => listView.Id==$"{t.memberInfo.Owner.Name}_{t.memberInfo.Name}_ListView").Distinct()
                                .Select(listView => ((IModelViewHiddenActions)listView).HiddenActions)
                                .SelectMany(modelHiddenActions => t.attribute.Actions.Where(action => modelHiddenActions[action] == null)
                                    .Do(action => modelHiddenActions.EnsureNode<IModelActionLink>(action))))
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
            => manager.WhenApplication(application => application.WhenFrame(ViewType.DetailView)
                .SelectMany(frame => frame.View.AsDetailView().NestedListViews()
                    .SelectMany(editor => editor.MemberInfo.FindAttributes<ReadOnlyCollectionAttribute>()
                        .Select(attribute => (attribute, editor))
                        .Do(t => {
                            var nestedFrameView = t.editor.Frame.View;
                            nestedFrameView.AllowEdit[nameof(ReadOnlyCollectionAttribute)] = t.attribute.AllowEdit;
                            nestedFrameView.AllowDelete[nameof(ReadOnlyCollectionAttribute)] = t.attribute.AllowDelete;
                            nestedFrameView.AllowNew[nameof(ReadOnlyCollectionAttribute)] = t.attribute.AllowNew;
                            var linkUnlinkController = t.editor.Frame.GetController<LinkUnlinkController>();
                            if (linkUnlinkController != null) {
                                linkUnlinkController.Active[nameof(ReadOnlyCollectionAttribute)] = t.attribute.AllowLinkUnLink;   
                            }
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

        static IObservable<Unit> DetailCollectionAttribute(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenSetupComplete()
                .SelectMany(_ => application.TypesInfo.PersistentTypes.Where(info => !info.IsAbstract)
                    .AttributedMembers<DetailCollectionAttribute>().ToObservable()
                    .SelectMany(t => application.SynchronizeNestedListViewSource(
                        t.memberInfo.Owner.FindMember(t.attribute.MasterCollectionName),
                        t.memberInfo, t.attribute.ChildPropertyName))));
        static IObservable<Unit> ReadOnlyObjectViewAttribute(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViews>().SelectMany().OfType<IModelObjectView>()
                .SelectMany(view => view.ModelClass.TypeInfo.FindAttributes<ReadOnlyObjectViewAttribute>()
                    .Where(attribute => view.Is( attribute.ViewType)).ToArray()
                    .Execute(attribute => {
                        view.AllowEdit = attribute.AllowEdit;
                        view.AllowDelete = attribute.AllowDelete;
                        view.AllowNew = attribute.AllowNew;
                        var modelHiddenActions = ((IModelViewHiddenActions) view).HiddenActions;
                        if (!view.AllowEdit) {
                            modelHiddenActions.EnsureNode<IModelActionLink>("Save");
                        }
                        if (attribute.DisableListViewProcess && view is IModelListView) {
                            modelHiddenActions.EnsureNode<IModelActionLink>(ListViewProcessCurrentObjectController.ListViewShowObjectActionId);
                        }
                    }).ToObservable(Transform.ImmediateScheduler))
                .ToUnit()
                .Merge(manager.WhenCustomizeTypesInfo()
                    .SelectMany(e => e.TypesInfo.Members<ReadOnlyObjectViewAttribute>().ToObservable().Where(t1 => t1.info.IsList)
                        .GroupBy(t2 => t2.info.ListElementTypeInfo).Select(ts => ts.Key)
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
