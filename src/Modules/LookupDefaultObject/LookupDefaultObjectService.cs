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

namespace Xpand.XAF.Modules.LookupDefaultObject{
    public static class LookupDefaultObjectService{
        public static SingleChoiceAction LookupDefaultObject(this (LookupDefaultObjectModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(LookupDefaultObject)).As<SingleChoiceAction>();
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager){
            var registerAction = manager.RegisterAction();
            
            return registerAction.Activate().ToUnit()
                .Merge(registerAction.SaveDefaultLookupObject())
                .Merge(manager.AssignDefaultLokupObject())
                .ToUnit();
        }

        private static IObservable<Unit> AssignDefaultLokupObject(this ApplicationModulesManager manager) => manager
	        .WhenApplication().WhenDetailViewCreated().ToDetailView()
	        .Where(view => view.Model.Application.IsDefaultObjectView(view.Id))
	        .SelectMany(view => view.Model.Application.ModelLookupDefaultObject().Items
		        .Where(item => item.ObjectView == view.Model)
		        .SelectMany(item => item.Members)
		        .Select(item => {
			        var memberInfo = item.MemberViewItem.ModelMember.MemberInfo;
			        var defaultObject = view.ObjectSpace.GetObjectsQuery<BusinessObjects.LookupDefaultObject>()
				        .FirstOrDefault(o => o.MemberName == memberInfo.Name && o.ObjectView == view.Id);
			        if (defaultObject != null){
				        var typeConverter = TypeDescriptor.GetConverter(memberInfo.MemberTypeInfo.KeyMember.MemberType);
				        var objectKeyValue = defaultObject.KeyValue;
				        var value = objectKeyValue!=null?view.ObjectSpace.GetObjectByKey(memberInfo.MemberType,
					        typeConverter.ConvertFromString(objectKeyValue)):null;
				        memberInfo.SetValue(view.CurrentObject, value);
				        return item;
			        }
			        return null;
		        }))
	        .WhenNotDefault()
	        .TraceLookupDefaultObject(item => item.Id())
	        .ToUnit();

        private static IObservable<Unit> SaveDefaultLookupObject(this IObservable<SingleChoiceAction> registerAction) => registerAction
            .WhenExecute()
            .Select(e => {
                var item = ((IModelLookupDefaultObjectObjectViewItem) e.SelectedChoiceActionItem.Data);
                using (var objectSpace = e.Action.Application.CreateObjectSpace()){
                    var defaultObjectItem = item.GetParent<IModelLookupDefaultObjectItem>();
                    var memberInfo = item.MemberViewItem.ModelMember.MemberInfo;
                    var memberName = memberInfo.Name;
                    var objectViewId = defaultObjectItem.ObjectView.Id;
                    var defaultObject = objectSpace.GetObjectsQuery<BusinessObjects.LookupDefaultObject>(true)
                                            .FirstOrDefault(o => o.MemberName == memberName && o.ObjectView == objectViewId) ??
                                        objectSpace.CreateObject<BusinessObjects.LookupDefaultObject>();
                    defaultObject.ObjectView = objectViewId;
                    defaultObject.MemberName = memberName;
                    var value = memberInfo.GetValue(e.SelectedObjects.Cast<object>().First());
                    defaultObject.KeyValue=value!=null?$"{objectSpace.GetKeyValue(value)}":null;
                    objectSpace.CommitChanges();
                }

                return item;
            })
            .TraceLookupDefaultObject(item => item.Id())
            .ToUnit();

        private static IObservable<SingleChoiceAction> RegisterAction(this ApplicationModulesManager manager) => manager
            .RegisterViewSingleChoiceAction(nameof(LookupDefaultObject), action => {
	            action.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
                action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
                action.Caption = "Default";
                action.ImageName = "Editor_Add";
                action.PaintStyle=ActionItemPaintStyle.CaptionAndImage;
            }).Publish().RefCount();

        private static IObservable<SingleChoiceAction> AddItems(this IObservable<SingleChoiceAction> activate) => activate
                .Do(action => {
                    action.Items.Clear();
                    foreach (var item in action.View().Model.Application.ModelLookupDefaultObject().Items.SelectMany(item => item.Members)){
                        action.Items.Add(new ChoiceActionItem(item.MemberViewItem.Caption, item));
                    }
                });

        private static IObservable<SingleChoiceAction> Activate(this IObservable<SingleChoiceAction> registerViewSingleChoiceAction) => registerViewSingleChoiceAction
            .WhenControllerActivated()
            .Do(action => action.Active[nameof(LookupDefaultObjectService)] = action.Application.Model.IsDefaultObjectView(action.View().Id))
            .AddItems()
            .WhenActive()
            .TraceLookupDefaultObject(action => action.Id);

        internal static bool IsDefaultObjectView(this IModelApplication applicationModel, string viewID) => applicationModel
            .ModelLookupDefaultObject().Items.Any(item => item.ObjectView.Id==viewID);

        internal static IObservable<TSource> TraceLookupDefaultObject<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, LookupDefaultObjectModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

    }
}