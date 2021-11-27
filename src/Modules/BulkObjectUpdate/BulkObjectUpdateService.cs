using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
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
            => manager.RegisterAction().AddItems(action => action.AddItems().ToUnit()
                .Concat(Observable.Defer(() => action.ShowView(update => update.UpdateObject())))).ToUnit();

        static IObservable<Unit> UpdateObject(this IObservable<(Frame listViewFrame, object o, DialogController dialogController)> source)
            => source.SelectMany(t => t.dialogController.AcceptAction.WhenExecuted(_ => t.dialogController.Frame.View.AsDetailView().GetItems<PropertyEditor>()
                        .Where(editor => ((IAppearanceEnabled)editor).Enabled&& ((IAppearanceVisibility)editor).Visibility==ViewItemVisibility.Show)
                        .Do(editor => {
                            var sourceValue = editor.MemberInfo.GetValue(editor.CurrentObject);
                            if (editor.MemberInfo.MemberTypeInfo.IsPersistent) {
                                sourceValue = t.listViewFrame.View.ObjectSpace.GetObject(sourceValue);
                            }
                            var member = t.listViewFrame.View.ObjectTypeInfo.FindMember(editor.MemberInfo.Name);
                            member.SetValue(t.o,sourceValue);
                        }).ToNowObservable()
                        .Finally(() => {
                            if (t.listViewFrame.Application.GetPlatform() == Platform.Win) {
                                t.listViewFrame.Action("Save")?.Active.SetItemValue("OnlyForDetailView", t.listViewFrame.View.ObjectSpace.IsModified);
                                t.dialogController.Frame.View.ObjectSpace.SetIsModified(false);    
                            }
                            else {
                                t.listViewFrame.View.ObjectSpace.CommitChanges();
                            }
                        }))
                )
                .ToUnit();

        static IObservable<Unit> ShowView(this SingleChoiceAction action,
            Func<IObservable<(Frame listViewFrame, object o, DialogController dialogController)>, IObservable<Unit>> update) 
            => action.WhenActive()
                .WhenExecuted(e => {
                    var showViewParameters = e.ShowViewParameters;
                    var application = e.Action.Application;
                    var objectSpace = application.CreateObjectSpace();
                    var modelDetailView = ((IModelBulkObjectUpdateRule)e.SelectedChoiceActionItem.Data).DetailView;
                    showViewParameters.CreatedView = application.CreateDetailView(objectSpace, modelDetailView.Id, true,
                        objectSpace.CreateObject(modelDetailView.ModelClass.TypeInfo.Type));
                    showViewParameters.TargetWindow=TargetWindow.NewModalWindow;
                    var dialogController = new DialogController();
                    dialogController.SaveOnAccept = false;
                    showViewParameters.Controllers.Add(dialogController);
                    return update(application.WhenViewOnFrame(modelDetailView.ModelClass.TypeInfo.Type).WhenFrame(ViewType.DetailView).FirstAsync()
                        .SelectMany(_ => e.SelectedObjects.Cast<object>().Select(o => (listView:e.Action.Controller.Frame,o, dialogController))));
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
        
        internal static IObservable<TSource> TraceObjectStateManager<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, BulkObjectUpdateModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
    }
}