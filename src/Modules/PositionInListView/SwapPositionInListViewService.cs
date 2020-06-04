using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.Data.Extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using DevExpress.Xpo.DB;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.PositionInListview{
    public static class SwapPositionInListViewService{
	    public const string EdgeContext = "On edge";
        public static SimpleAction MoveObjectUp(this (PositionInListViewModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(MoveObjectUp)).As<SimpleAction>();
        public static SimpleAction MoveObjectDown(this (PositionInListViewModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(MoveObjectDown)).As<SimpleAction>();
        internal static IObservable<Unit> SwapPosition(this ApplicationModulesManager manager){
            var registerActions = manager.RegisterActions();
            return registerActions.Activate().Enable()
	            .Merge(registerActions.SwapSelectedObject())
	            .ToUnit();
        }

        private static IObservable<SimpleAction> RegisterActions(this ApplicationModulesManager manager) =>
            new[]{nameof(MoveObjectUp), nameof(MoveObjectDown)}.ToObservable()
                .SelectMany(actionnId => manager.RegisterViewSimpleAction(actionnId, Configure, PredefinedCategory.RecordsNavigation))
                .TracePositionInListView(action => action.Id)
                .Publish().RefCount();

        private static void Configure(SimpleAction action){
	        action.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
	        action.ImageName = action.Id == nameof(MoveObjectUp) ? "Actions_Arrow3Up" : "Actions_Arrow3Down";
	        action.Caption = action.Id.Replace("Object", " ");
	        action.TargetViewType=ViewType.ListView;
        }

        static IObservable<Unit> SwapSelectedObject(this IObservable<SimpleAction> source) => source
            .WhenExecute().Do(_ => _.Action.SwapPosition(_.SelectedObjects.Cast<object>().First()))
            .TracePositionInListView(e => $"{e.Action.Id}, {string.Join(", ",e.SelectedObjects.Cast<object>().Select(o => o.ToString()))}")
            .ToUnit();

        private static void SwapPosition(this ActionBase action,  object selectedObject){
            var listView = action.View<ListView>();
            var positionMember = action.Application.Model.ModelPositionInListView().ListViewItems
                .First(item => item.ListView.Id == listView.Id).PositionMember.MemberInfo;
            var objects = listView.Objects();
            var selectedIndex = objects.FindIndex(o => selectedObject == o);
            var swapIndex = action.Id == nameof(MoveObjectUp) ? selectedIndex - 1 : selectedIndex + 1;
            if (swapIndex > -1 && swapIndex <= objects.Length - 1){
                var selectedPosition = positionMember.GetValue(selectedObject);
                var swapObject = objects[swapIndex];
                var swapPosition = positionMember.GetValue(swapObject);
                positionMember.SetValue(swapObject, selectedPosition);
                positionMember.SetValue(selectedObject, swapPosition);
            }
        }

        private static IObservable<SimpleAction> Activate(this IObservable<SimpleAction> source) => source.WhenActivated()
            .Do(action => action.Active[nameof(PositionInListViewService)] = action.Application.Model.IsPositionInListView(action.View().Id))
            .WhenActive()
            .TracePositionInListView(action => action.Id);

        private static IObservable<Unit> Enable(this IObservable<SimpleAction> source) => source
	        .SelectMany(action => action.View<ListView>().WhenSelectionChanged().Pair(action))
	        .Merge(source.WhenExecute()
		        .Do(_ => {
			        _.Action.Controller.Frame.Action(nameof(MoveObjectDown)).Enabled[EdgeContext] = true;
			        _.Action.Controller.Frame.Action(nameof(MoveObjectUp)).Enabled[EdgeContext] = true;
		        })
		        .Select(_ => (listview:_.Action.View<ListView>(),action:_.Action.As<SimpleAction>())))
	        .Do(_ => {
		        var listView = _.Item1;
		        var action = _.Item2;
		        var currentObject = listView.SelectedObjects.Cast<object>().FirstOrDefault();
		        if (currentObject != null){
			        var objects = listView.Objects();
			        var keyValue = listView.ObjectSpace.GetKeyValue(currentObject).ToString();
			        var selectedIndex = objects.FindIndex(o => listView.ObjectSpace.GetKeyValue(o).ToString() == keyValue);
			        action.Enabled[EdgeContext] = action.Id == nameof(MoveObjectUp)?selectedIndex > 0:selectedIndex<objects.Length-1;
				        
		        }
	        })
	        .TracePositionInListView(_ => _.Item2.Id)
	        .ToUnit();

        private static object[] Objects(this ListView listView){
	        var objects = listView.CollectionSource.Objects();
	        if (listView.CollectionSource.CanApplySorting){
		        return objects.ToArray();
	        }
	        var item = listView.Model.Application.ModelPositionInListView().ListViewItems
		        .First(rule => rule.ListView == listView.Model);
	        return item.SortingDirection == SortingDirection.Ascending
		        ? objects.OrderBy(o => item.PositionMember.MemberInfo.GetValue(o)).ToArray()
		        : objects.OrderByDescending(o => item.PositionMember.MemberInfo.GetValue(o)).ToArray();
        }
    }
}