using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.BulkObjectUpdate{
    public static class BulkObjectUpdateService {
        public static SingleChoiceAction BulkUpdate(this (BulkObjectUpdateModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(BulkUpdate)).As<SingleChoiceAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.RegisterAction()
                .MergeIgnored(action => action.ShowView().UpdateListViewObjects())
                .AddItems(action => action.AddItems().ToUnit()).ToUnit();

        static IObservable<Unit> UpdateListViewObjects(this IObservable<(Frame listView, Frame detailView)> source) 
	        => source.SelectMany(t => t.detailView.GetController<DialogController>().AcceptAction.WhenExecuted(
		        _ => t.PropertyEditors().SelectMany(editor => t.listView.View.SelectedObjects.Cast<object>()
				        .Do(o => {
					        var sourceValue = editor.MemberInfo.GetValue(editor.CurrentObject);
					        if (editor.MemberInfo.MemberTypeInfo.IsPersistent) {
						        sourceValue = t.listView.View.ObjectSpace.GetObject(sourceValue);
					        }
                            editor.MemberInfo.SetValue(o, sourceValue);
				        }))
			        .ToNowObservable()
			        .Finally(() => {
                        t.detailView.View.ObjectSpace.SetIsModified(false);
				        if (t.listView.Application.GetPlatform() != Platform.Win||t.listView.View.IsRoot) {
					        t.listView.View.ObjectSpace.CommitChanges();
				        }
                    }))).ToUnit();

        private static IEnumerable<PropertyEditor> PropertyEditors(this (Frame listView, Frame detailView) t) 
	        => t.detailView.View.AsDetailView().GetItems<PropertyEditor>()
                .Where(editor => ((IAppearanceVisibility)editor).Visibility == ViewItemVisibility.Show&&editor.Model.LayoutItem()!=null);

        static IObservable<(Frame listView, Frame detailView)> ShowView(this SingleChoiceAction action) 
            => action.WhenExecuted(e => {
                    var showViewParameters = e.ShowViewParameters;
                    var application = e.Action.Application;
                    var modelDetailView = ((IModelBulkObjectUpdateRule)e.SelectedChoiceActionItem.Data).DetailView;
                    var objectSpace = application.CreateObjectSpace(modelDetailView.ModelClass.TypeInfo.Type);
                    showViewParameters.CreatedView = application.CreateDetailView(objectSpace, modelDetailView.Id, true,
                        objectSpace.CreateObject(modelDetailView.ModelClass.TypeInfo.Type));
                    showViewParameters.TargetWindow=TargetWindow.NewModalWindow;
                    var dialogController = e.Application().CreateController<DialogController>();
                    dialogController.SaveOnAccept = false;
                    showViewParameters.Controllers.Add(dialogController);
                    return application.WhenViewOnFrame(modelDetailView.ModelClass.TypeInfo.Type).WhenFrame(ViewType.DetailView).FirstAsync()
	                    .Select(frame => (listView:e.Action.Controller.Frame,detailView:frame));
                });

        private static IObservable<SingleChoiceAction> RegisterAction(this ApplicationModulesManager manager) 
            => manager.RegisterViewSingleChoiceAction(nameof(BulkUpdate),action => action.ConfigureAction());

        private static void ConfigureAction(this SingleChoiceAction action) {
            action.SelectionDependencyType=SelectionDependencyType.RequireMultipleObjects;
            action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
            action.TargetViewType=ViewType.ListView;
            action.ImageName = "Action_Change_State";
        }

        private static IObservable<ChoiceActionItem> AddItems(this SingleChoiceAction action)
            => action.Model.Application.ModelObjectStateManager().Rules.Where(rule => action.View().Model==rule.ListView)
                .Select(rule => new ChoiceActionItem(rule.Caption, rule)).ToNowObservable()
                .Do(item => action.Items.Add(item)).TraceObjectStateManager(item => item.Caption);
        
        internal static IObservable<TSource> TraceObjectStateManager<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, BulkObjectUpdateModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
    }
}