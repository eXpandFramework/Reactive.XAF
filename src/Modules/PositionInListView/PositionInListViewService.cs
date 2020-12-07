using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.Data;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.PositionInListView{
    public static class PositionInListViewService{
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) =>
            manager.WhenApplication(application => {
                var positionInListCreated = application.WhenPositionInListCreated();
                return positionInListCreated.SortCollectionSource()
                    .Merge(positionInListCreated.DisableSorting())
                    .Merge(application.ReturnObservable().PositionNewObjects());
            });

        internal static IObservable<TSource> TracePositionInListView<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
	        source.Trace(name, PositionInListViewModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName);


        private static IObservable<(ListView listView, XafApplication application)> WhenPositionInListCreated(this XafApplication application) =>
            application.WhenListViewCreated()
                .Where(_ => application.Model.IsPositionInListView(_.Model.Id))
                .Pair(application)
                .Publish().RefCount();

        private static IObservable<Unit> DisableSorting(this IObservable<(ListView listView, XafApplication application)> whenPositionInListCreated) =>
            whenPositionInListCreated
                .SelectMany(_ => _.listView.WhenControlsCreated())
                .SelectMany(view => ((ColumnsListEditor) view.Editor).Columns)
                .Do(wrapper => {
                    wrapper.AllowGroupingChange = false;
                    wrapper.AllowSortingChange = false;
                })
                .TracePositionInListView(wrapper => wrapper.Caption)
                .ToUnit();

        private static IObservable<Unit> SortCollectionSource(this IObservable<(ListView listView, XafApplication application)> whenPositionInListCreated) =>
            whenPositionInListCreated
                .Where(tuple => tuple.application.Model.IsPositionInListView( tuple.listView.Id))
                .Select(_ => {
                    var item = _.application.Model.ModelPositionInListView().ListViewItems
                        .First(rule => rule.ListView.Id() == _.listView.Id);
                    foreach (var listViewColumn in item.ListView.Columns){
                        listViewColumn.SortOrder=ColumnSortOrder.None;
                    }

                    if (!_.listView.CollectionSource.CanApplySorting){
	                    var modelColumn = _.listView.Model.Columns.FirstOrDefault(column => column.ModelMember==item.PositionMember);
	                    if (modelColumn == null){
		                    modelColumn = _.listView.Model.Columns.AddNode<IModelColumn>();
		                    modelColumn.PropertyName = item.PositionMember.Name;
		                    modelColumn.Index = -1;
	                    }
	                    modelColumn.SortIndex = 0;
	                    modelColumn.SortOrder = (ColumnSortOrder) Enum.Parse(typeof(ColumnSortOrder), item.SortingDirection.ToString());
                    }
                    
                    _.listView.CollectionSource.Sorting.Insert(0, new SortProperty(item.PositionMember.MemberInfo.Name, item.SortingDirection));
                    return _.listView;
                })
                .TracePositionInListView(view => view.Id)
                .ToUnit();

        internal static bool IsPositionInListView(this IModelApplication applicationModel, string viewID) => applicationModel
            .ModelPositionInListView().ListViewItems.Select(item => item.ListView.Id()).Contains(viewID);

        private static IObservable<Unit> PositionNewObjects(this IObservable<XafApplication> whenApplication) =>
            whenApplication.SelectMany(application => application.WhenNewObjectCreated<object>().Select(tuple => tuple).Pair(application))
                .Do(_ => {
                    var modelPositionInListView = _.other.Model.ModelPositionInListView();
                    var listViewItems = modelPositionInListView.ListViewItems;
                    if (listViewItems.Any()){
                        var objectType = _.source.theObject.GetType();
                        var listViewItem = listViewItems.FirstOrDefault(item => item.ListView.ModelClass.TypeInfo.Type==objectType);
                        if (listViewItem != null){
                            var modelClassItem = modelPositionInListView.ModelClassItems
                                .FirstOrDefault(item => item.ModelClass.TypeInfo.Type == objectType && item.ModelMember == listViewItem.PositionMember);
                            var aggregate = Aggregate.Max;
                            if (modelClassItem != null&&modelClassItem.NewObjectsStrategy==PositionInListViewNewObjectsStrategy.First){
                                aggregate=Aggregate.Min;
                            }
                            var memberInfo = listViewItem.PositionMember.MemberInfo;
                            var aggregateOperand = new AggregateOperand("", memberInfo.Name, aggregate);
                            var value = (int)(_.source.objectSpace.Evaluate(objectType, aggregateOperand, null) ?? 0) ;
                            var allValues = _.source.objectSpace.ModifiedObjects.Cast<object>().Select(o => memberInfo.GetValue(o)??0).Cast<int>().Add(value);
                            if (aggregate == Aggregate.Max){
                                value = allValues.Max();
                                value++;    
                            }
                            else{
                                value = allValues.Min();
                                value--;
                            }
                            memberInfo.SetValue(_.source.theObject,value);
                        }                    
                    }
                })
                .TracePositionInListView(_ => _.source.theObject.ToString())
                .ToUnit();

        
    }
}