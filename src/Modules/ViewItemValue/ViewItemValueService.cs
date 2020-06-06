using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Templates;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.ViewItemValue{
    public static class ViewItemValueService{
        public static SingleChoiceAction ViewItemValue(this (ViewItemValueModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(ViewItemValue)).As<SingleChoiceAction>();
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager){
            var registerAction = manager.RegisterAction();
            
            return registerAction.Activate().ToUnit()
                .Merge(registerAction.SaveViewItemValue())
                .Merge(manager.AssignViewItemValue())
                .ToUnit();
        }

        private static IObservable<Unit> AssignViewItemValue(this ApplicationModulesManager manager) => manager
	        .WhenApplication().WhenDetailViewCreated().ToDetailView()
	        .Where(view => view.Model.Application.IsViewItemValueObjectView(view.Id))
	        .SelectMany(view => view.Model.Application.ModelViewItemValue().Items
		        .Where(item => item.ObjectView == view.Model)
		        .SelectMany(item => item.Members)
		        .Select(item => {
			        var memberInfo = item.MemberViewItem.ModelMember.MemberInfo;
			        var defaultObject = view.ObjectSpace.GetObjectsQuery<BusinessObjects.ViewItemValueObject>()
				        .FirstOrDefault(o => o.ViewItemId == memberInfo.Name && o.ObjectView == view.Id);
			        if (defaultObject != null){
				        var typeConverter = TypeDescriptor.GetConverter(memberInfo.MemberTypeInfo.KeyMember.MemberType);
				        var objectKeyValue = defaultObject.ViewItemValue;
				        var value = objectKeyValue!=null?view.ObjectSpace.GetObjectByKey(memberInfo.MemberType,
					        typeConverter.ConvertFromString(objectKeyValue)):null;
				        memberInfo.SetValue(view.CurrentObject, value);
				        return item;
			        }
			        return null;
		        }))
	        .WhenNotDefault()
	        .TraceDefaultObjectValue(item => item.Id())
	        .ToUnit();

        private static IObservable<Unit> SaveViewItemValue(this IObservable<SingleChoiceAction> registerAction) => registerAction
            .WhenExecute()
            .Select(e => {
                var item = ((IModelViewItemValueObjectViewItem) e.SelectedChoiceActionItem.Data);
                using (var objectSpace = e.Action.Application.CreateObjectSpace()){
                    var defaultObjectItem = item.GetParent<IModelViewItemValueItem>();
                    var memberInfo = item.MemberViewItem.ModelMember.MemberInfo;
                    var memberName = memberInfo.Name;
                    var objectViewId = defaultObjectItem.ObjectView.Id;
                    var defaultObject = objectSpace.GetObjectsQuery<BusinessObjects.ViewItemValueObject>(true)
                                            .FirstOrDefault(o => o.ViewItemId == memberName && o.ObjectView == objectViewId) ??
                                        objectSpace.CreateObject<BusinessObjects.ViewItemValueObject>();
                    defaultObject.ObjectView = objectViewId;
                    defaultObject.ViewItemId = memberName;
                    var value = memberInfo.GetValue(e.SelectedObjects.Cast<object>().First());
                    defaultObject.ViewItemValue=value!=null?$"{objectSpace.GetKeyValue(value)}":null;
                    objectSpace.CommitChanges();
                }

                return item;
            })
            .TraceDefaultObjectValue(item => item.Id())
            .ToUnit();

        private static IObservable<SingleChoiceAction> RegisterAction(this ApplicationModulesManager manager) => manager
            .RegisterViewSingleChoiceAction(nameof(ViewItemValue), action => {
	            action.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
                action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
                action.Caption = "Default";
                action.ImageName = "Editor_Add";
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

        internal static bool IsViewItemValueObjectView(this IModelApplication applicationModel, string viewID) => applicationModel
            .ModelViewItemValue().Items.Any(item => item.ObjectView.Id==viewID);

        internal static IObservable<TSource> TraceDefaultObjectValue<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, ViewItemValueModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

    }
}