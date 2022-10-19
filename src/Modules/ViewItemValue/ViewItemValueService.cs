using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Templates;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.ViewItemValue.BusinessObjects;

namespace Xpand.XAF.Modules.ViewItemValue{
    public static class ViewItemValueService{
        public static SingleChoiceAction ViewItemValue(this (ViewItemValueModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(ViewItemValue)).As<SingleChoiceAction>();
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager){
            var registerAction = manager.RegisterAction();
            return registerAction.Activate().ToUnit()
                .Merge(registerAction.SaveViewItemValue())
                .MergeToUnit(manager.WhenViewItemValueItem()
                    .Publish(source => source.Select(t => t.model.AssignViewItemValue( t.frame.View.ToDetailView()))
                        .MergeToUnit(source.SaveViewItemValueOnCommit())))
                .ToUnit();
        }

        private static IObservable<Unit> SaveViewItemValueOnCommit(this IObservable<(IModelViewItemValueObjectViewItem model, Frame frame)> source) 
            => source.Where(t => t.model.SaveViewItemValueStrategy==SaveViewItemValueStrategy.OnCommit).SelectMany(t => t.frame.View.ObjectSpace.WhenCommitted()
                    .Do(_ => t.frame.SingleChoiceAction(nameof(ViewItemValue)).DoExecute(t.model)))
                .ToUnit();

        private static IObservable<(IModelViewItemValueObjectViewItem model, Frame frame)> WhenViewItemValueItem(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenFrame(ViewType.DetailView)
                .Where(frame => frame.View.Model.Application.IsViewItemValueObjectView(frame.View.Id))
                .SelectUntilViewClosed(frame => frame.View.WhenCurrentObjectChanged().To(frame).StartWith(frame)
                    .SelectMany(_ => frame.View.Model.Application.ModelViewItemValue().Items
                        .Where(item => item.ObjectView == frame.View.Model)
                        .SelectMany(item => item.Members.Select(viewItem => (model:viewItem, frame)).ToArray()))));

        private static IModelViewItemValueObjectViewItem AssignViewItemValue(this IModelViewItemValueObjectViewItem item, DetailView view) {
            var memberInfo = item.MemberViewItem.ModelMember.MemberInfo;
            var defaultObject = view.ViewItemValueObject(memberInfo);
            if (defaultObject != null) {
                var memberValue = defaultObject.ViewItemValue;
                if (memberInfo.MemberTypeInfo.IsDomainComponent) {
                    var typeConverter = TypeDescriptor.GetConverter(memberInfo.MemberTypeInfo.KeyMember.MemberType);
                    var value = memberValue != null
                        ? view.ObjectSpace.GetObjectByKey(memberInfo.MemberType, typeConverter.ConvertFromString(memberValue)) : null;
                    memberInfo.SetValue(view.CurrentObject, value);
                }
                else {
                    var typeConverter = TypeDescriptor.GetConverter(memberInfo.MemberType);
                    memberInfo.SetValue(view.CurrentObject, typeConverter.ConvertFromString(memberValue));
                }

                return item;
            }

            return null;
        }

        private static ViewItemValueObject ViewItemValueObject(this DetailView view, IMemberInfo memberInfo){
            ViewItemValueObject Query(IObjectSpace space) 
                => space.GetObjectsQuery<ViewItemValueObject>()
                    .FirstOrDefault(o => o.ViewItemId == memberInfo.Name && o.ObjectView == view.Id);
            if (!view.ObjectTypeInfo.IsPersistent) {
                using var space = view.Application().CreateObjectSpace(typeof(ViewItemValueObject));
                return Query(space);
            }
            return Query(view.ObjectSpace);
        }

        private static IObservable<Unit> SaveViewItemValue(this IObservable<SingleChoiceAction> registerAction) 
            => registerAction.WhenExecute()
            .Select(e => {
                var item = ((IModelViewItemValueObjectViewItem) e.SelectedChoiceActionItem.Data);
                var application = e.Action.Application;
                using var objectSpace = application.CreateObjectSpace(typeof(ViewItemValueObject));
                var defaultObjectItem = item.GetParent<IModelViewItemValueItem>();
                var memberInfo = item.MemberViewItem.ModelMember.MemberInfo;
                var memberName = memberInfo.Name;
                var objectViewId = defaultObjectItem.ObjectView.Id;
                var viewItemValueObject = objectSpace.GetObjectsQuery<ViewItemValueObject>(true)
                                        .FirstOrDefault(o => o.ViewItemId == memberName && o.ObjectView == objectViewId) ??
                                    objectSpace.CreateObject<ViewItemValueObject>();
                viewItemValueObject.ObjectView = objectViewId;
                viewItemValueObject.ViewItemId = memberName;
                var value = memberInfo.GetValue(e.SelectedObjects.Cast<object>().First());
                viewItemValueObject.ViewItemValue=memberInfo.MemberTypeInfo.IsPersistent? value!=null?$"{objectSpace.GetKeyValue(value)}":null:$"{value}";
                objectSpace.CommitChanges();

                return item;
            })
            .TraceDefaultObjectValue(item => item.Id())
            .ToUnit();

        private static IObservable<SingleChoiceAction> RegisterAction(this ApplicationModulesManager manager) => manager
            .RegisterViewSingleChoiceAction(nameof(ViewItemValue), action => {
	            action.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
                action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
                action.Caption = "Default";
                action.ImageName = "DifferentFirstPage";
                action.PaintStyle=ActionItemPaintStyle.CaptionAndImage;
            }).Publish().RefCount();

        private static IObservable<SingleChoiceAction> AddItems(this IObservable<SingleChoiceAction> activate) => activate
                .Do(action => {
                    action.Items.Clear();
                    var modelView = action.View().Model;
                    var items = modelView.Application.ModelViewItemValue().Items
	                    .Where(item => item.ObjectView==modelView);
                    foreach (var item in items.SelectMany(item => item.Members)){
                        action.Items.Add(new ChoiceActionItem(item.MemberViewItem.Caption, item));
                    }
                });

        private static IObservable<SingleChoiceAction> Activate(this IObservable<SingleChoiceAction> registerViewSingleChoiceAction) => registerViewSingleChoiceAction
            .WhenControllerActivated()
            .Do(action => action.Active[nameof(ViewItemValueService)] = action.Application.Model.IsViewItemValueObjectView(action.View().Id))
            .AddItems()
            .WhenActive()
            .TraceDefaultObjectValue(action => action.Id);

        internal static bool IsViewItemValueObjectView(this IModelApplication applicationModel, string viewID) 
            => applicationModel.ModelViewItemValue().Items.Any(item => item.ObjectViewId==viewID);

        internal static IObservable<TSource> TraceDefaultObjectValue<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, ViewItemValueModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

    }
}