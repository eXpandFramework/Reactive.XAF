

using System;
using System.Collections;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Appearance;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services;
public static partial class AttributesExtensions {
    private static IObservable<Unit> QuickAccessNavigationItemActions(this ApplicationModulesManager manager) 
        => manager.ExportedTypes().Attributed<NavigationItemQuickAccessAttribute>().ToNowObservable()
            .SelectMany(t => manager.RegisterWindowSimpleAction(t.typeInfo.Name,PredefinedCategory.ViewsNavigation,
                    action => {
                        action.QuickAccess = true;
                        action.PaintStyle=ActionItemPaintStyle.Image;
                        action.ImageName=t.typeInfo.Name;
                        action.TargetViewNesting=t.attribute.Nesting;
                        if (t.attribute.Index != null){
                            action.Model.Index = t.attribute.Index.Value;
                        }
                    })
                .ActivateFor(TemplateContext.ApplicationWindow)
                .WhenExecuted(e => {
                    var viewId = t.attribute.ViewId;
                    return viewId != null ? e.Application().Navigate(viewId) : e.Application().Navigate(t.typeInfo.Type);
                })
            )
            .ToUnit();

    static IObservable<Unit> LinkUnlinkPropertyAttribute(this ApplicationModulesManager manager)
        => manager.WhenApplication(application => application.WhenFrame(frame => frame.View.ObjectTypeInfo.AttributedMembers<LinkUnlinkPropertyAttribute>().ToNowObservable()
            .SelectMany(t => frame.View.AsDetailView().NestedListViews(t.memberInfo.ListElementType)
                .SelectMany(editor => editor.WhenLinkUnlinkAction( frame, t)
                    .Merge(editor.WhenNewObjectAction( frame, t)))
            ),typeof(object),ViewType.DetailView) ) ;

    private static IObservable<Unit> WhenNewObjectAction(this ListPropertyEditor editor, Frame frame, (LinkUnlinkPropertyAttribute attribute, IMemberInfo memberInfo) t) 
        => editor.Frame.NewObjectAction().WhenExecuted()
            .DoItemResilient(e => {
                var createdView = e.ShowViewParameters.CreatedView;
                var associatedCollection = frame.View.ObjectTypeInfo.FindMember(t.attribute.PropertyName);
                if (!associatedCollection.IsAggregated) return;
                var associatedMemberInfo = associatedCollection.AssociatedMemberInfo;
                if (associatedMemberInfo.GetValue(createdView.CurrentObject) != null) return;
                associatedMemberInfo.SetValue(createdView.CurrentObject,createdView.ObjectSpace.GetObject(frame.View.CurrentObject));
            })
            .ToUnit();

    private static IObservable<Unit> WhenLinkUnlinkAction(this ListPropertyEditor editor, Frame frame, (LinkUnlinkPropertyAttribute attribute, IMemberInfo memberInfo) t){
        var controller = editor.Frame.GetController<LinkUnlinkController>();
        return controller.LinkAction.WhenExecuteCompleted(e => e.PopupWindowViewSelectedObjects.Cast<object>().ToNowObservable()
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
                }))
            .MergeToUnit(controller.UnlinkAction.WhenExecuteCompleted(e => e.SelectedObjects.Cast<object>()
                .Do(value => ((IList)frame.View.ObjectTypeInfo.FindMember(t.attribute.PropertyName)
                    .GetValue(frame.View.CurrentObject)).Remove(value)).ToNowObservable()));
    }
        
    static IObservable<Unit> DisableNewObjectAction(this ApplicationModulesManager manager)
        => manager.WhenApplication(application => application.WhenFrameCreated().ToController<NewObjectViewController>()
                .SelectManyItemResilient(controller => controller.WhenCollectDescendantTypes()
                    .SelectMany(e => e.Types.Where(type => type.Attributes<DisableNewObjectActionAttribute>().Any()).ToArray()
                        .Do(type => e.Types.Remove(type)))))
            .ToUnit();
        
    static IObservable<Unit> HiddenActions(this ApplicationModulesManager manager)
        => manager.WhenGeneratingModelNodes<IModelViews>()
            .SelectManyItemResilient(views =>views.OfType<IModelObjectView>()
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
}