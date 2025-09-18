using System;
using System.Collections;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.XAF.Modules.Reactive.Services {
public static partial class AttributesExtensions {
        static IObservable<Unit> ColumnSorting(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelColumns>()
                .SelectMany(columns => columns.SelectMany(column =>
                    column.ModelMember.MemberInfo.FindAttributes<ColumnSortingAttribute>(true)
                        .Do(attribute => {
                            column.SortIndex = attribute.SortIndex;
                            column.SortOrder=attribute.SortOrder;
                        }).ToUnit()))
                .ToUnit();
        
        static IObservable<Unit> EditorAliasDisabledInDetailViewAttribute(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViewItems>()
                .SelectMany(items => items.OfType<IModelPropertyEditor>().Where(editor => editor.ModelMember.MemberInfo.FindAttribute<EditorAliasAttribute>()!=null)
                    .Do(editor => {
                        var attribute = editor.ModelMember.MemberInfo.FindAttribute<EditorAliasDisabledInDetailViewAttribute>();
                        if (attribute==null)return;
                        editor.PropertyEditorType = editor.ModelMember.CalculateEditorType();
                    }))
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
                        .SelectMany(types => application.WhenFrame(frame => frame.View.WhenControlsCreated(true)
                                .Select(view => view.ToListView().Editor.GridView())
                                .SelectMany(gridView => types.SelectMany(t1 => {
                                    var column = gridView.GetPropertyValue("Columns")
                                        .CallMethod("ColumnByFieldName", t1.memberInfo.BindingName);
                                    return column==null?[]: ((IEnumerable)column
                                            .GetPropertyValue("Summary")).Cast<object>()
                                        .Do(item => item.SetPropertyValue("Mode", t1.attribute.SummaryMode));
                                })),types.Key.Type,ViewType.ListView)
                            
                        ))));
        
        static IObservable<Unit> VisibleInAllViewsAttribute(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViewItems>()
                .SelectManyItemResilient(items => items.GetParent<IModelDetailView>().ModelClass.TypeInfo.AttributedMembers<VisibleInAllViewsAttribute>(attribute => attribute.CreateModelMember)
                    .Where(t => !items.OfType<IModelPropertyEditor>().Select(editor => editor.ModelMember.MemberInfo).Contains(t.memberInfo)).ToArray()
                    .Do(t => items.AddNode<IModelPropertyEditor>(t.memberInfo.Name).PropertyName = t.memberInfo.Name).ToNowObservable())
                .ToUnit();
        
        static IObservable<Unit> ListViewShowFooterCollection(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViews>()
                .SelectManyItemResilient(views => views.OfType<IModelListView>()).Where(view =>
                    view.ModelClass.TypeInfo.FindAttributes<ListViewShowFooterAttribute>(true).Any())
                .Do(view => view.IsFooterVisible = true)
                .ToUnit();
        
        static IObservable<Unit> ReadOnlyProperty(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelBOModelClassMembers>()
                .SelectManyItemResilient(members => members.SelectMany(member => member.MemberInfo.FindAttributes<ReadOnlyPropertyAttribute>()
                    .Do(attribute => {
                        member.AllowClear = attribute.AllowClear;
                        member.AllowEdit = false;
                    })))
                .ToUnit();
        
        static IObservable<Unit> ReadOnlyCollection(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenFrame(ViewType.DetailView)
                .SelectMany(frame => frame.View.AsDetailView().NestedListViews()
                    .SelectManyItemResilient(editor => editor.MemberInfo.FindAttributes<ReadOnlyCollectionAttribute>()
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

        static IObservable<Unit> AppearanceToolAttribute(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenFrameCreated().ToController<AppearanceController>()
                .SelectMany(controller => controller.ProcessEvent<CustomCreateAppearanceRuleEventArgs,CustomCreateAppearanceRuleEventArgs>(nameof(AppearanceController.CustomCreateAppearanceRule),
                        e => e.Observe().Where(_ => e.RuleProperties is IModelAppearanceWithToolTipRule)
                            .Do(_ => e.Rule = new ToolTipAppearanceRule(e.RuleProperties, e.ObjectSpace)))
                    .MergeToUnit(controller.ProcessEvent<ApplyAppearanceEventArgs,ApplyAppearanceEventArgs>(nameof(AppearanceController.AppearanceApplied),
                        e => e.Observe().Do(_ => {
                                if (e.AppearanceObject.Items.FirstOrDefault(item => item is AppearanceItemToolTip) is not AppearanceItemToolTip toolTip) return;
                                if (toolTip.State == AppearanceState.None) return;
                                var toolTipText = toolTip.State == AppearanceState.CustomValue ?  toolTip.ToolTipText :  "";
                                if (e.Item is not ColumnWrapper columnWrapper) return;
                                columnWrapper.ToolTip = toolTipText;
                            })) )))
                .ToUnit();
        
        static IObservable<Unit> DetailCollectionAttribute(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenSetupComplete()
                .SelectMany(_ => application.TypesInfo.PersistentTypes.Where(info => !info.IsAbstract)
                    .AttributedMembers<DetailCollectionAttribute>().ToNowObservable()
                    .SelectManyItemResilient(t => application.SynchronizeNestedListViewSource(
                        t.memberInfo.Owner.FindMember(t.attribute.MasterCollectionName),
                        t.memberInfo, t.attribute.ChildPropertyName))));
        
        static IObservable<Unit> ReadOnlyObjectViewAttribute(this ApplicationModulesManager manager)
            => manager.WhenGeneratingModelNodes<IModelViews>().SelectMany().OfType<IModelObjectView>()
                .SelectManyItemResilient(view => view.ModelClass.TypeInfo.FindAttributes<ReadOnlyObjectViewAttribute>()
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
                    .SelectMany(e => e.TypesInfo.Members<ReadOnlyObjectViewAttribute>().ToNowObservable().Where(t1 => t1.info.IsList)
                        .GroupBy(t2 => t2.info.ListElementTypeInfo).Select(ts => ts.Key)
                        .DoItemResilient(info => ((TypeInfo) info).AddAttribute(new ReadOnlyObjectViewAttribute()))
                        .ToUnit()));
        
        
    }}