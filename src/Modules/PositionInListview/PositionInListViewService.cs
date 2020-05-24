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
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Model;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.PositionInListview{
    public static class PositionInListViewService{
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager){
            var whenApplication = manager.WhenApplication();
            var whenPositionInListCreated = WhenPositionInListCreated(whenApplication);
            return whenApplication.PositionNewObjects()
                .Merge(whenPositionInListCreated.SortCollectionSource())
                .Merge(whenPositionInListCreated.DisableSorting());
        }
        internal static IObservable<TSource> TracePositionInListView<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
	        source.Trace(name, PositionInListViewModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        private static IObservable<(ListView listView, XafApplication application)> WhenPositionInListCreated(this IObservable<XafApplication> whenApplication) =>
            whenApplication.WhenListViewCreated()
                .Where(_ => _.application.Model.IsPositionInListView(_.listView.Model.Id)).Publish().RefCount();

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
                    _.listView.CollectionSource.Sorting.Insert(0, new SortProperty(item.PositionMember.MemberInfo.Name, item.SortingDirection));
                    return _.listView;
                })
                .TracePositionInListView(view => view.Id)
                .ToUnit();

        internal static bool IsPositionInListView(this IModelApplication applicationModel, string viewID) => applicationModel
            .ModelPositionInListView().ListViewItems.Select(item => item.ListView.Id()).Contains(viewID);

        private static IObservable<Unit> PositionNewObjects(this IObservable<XafApplication> whenApplication) =>
            whenApplication.WhenObjectSpaceCreated()
                .SelectMany(_ => _.e.ObjectSpace.WhenModifiedObjects(ObjectModification.New)
                .Do(tuple => {
                    var modelPositionInListView = _.application.Model.ModelPositionInListView();
                    foreach (var objects in tuple.objects.GroupBy(o => o.GetType())){
                        var listViewItem = modelPositionInListView.ListViewItems.First(item => item.ListView.ModelClass.TypeInfo.Type==objects.Key);
                        var modelClassItem = modelPositionInListView.ModelClassItems.FirstOrDefault(item => item.ModelClass.TypeInfo.Type==objects.Key&&item.ModelMember==listViewItem.PositionMember);
                        var aggregate = Aggregate.Max;
                        if (modelClassItem != null&&modelClassItem.NewObjectsStrategy==PositionInListViewNewObjectsStrategy.First){
                            aggregate=Aggregate.Min;
                        }
                        var aggregateOperand = new AggregateOperand("", listViewItem.PositionMember.MemberInfo.Name, aggregate);
                        var value = (int)(tuple.objectSpace.Evaluate(objects.Key, aggregateOperand, null) ?? 0) ;
                        foreach (var o in objects){
                            if (aggregate == Aggregate.Max){
                                value++;    
                            }
                            else{
                                value--;
                            }
                            listViewItem.PositionMember.MemberInfo.SetValue(o,value);
                        }
                    }
                }))
                .TracePositionInListView(_ => string.Join(", ",_.objects))
                .ToUnit();

        internal static IModelPositionInListView ModelPositionInListView(this IModelApplication modelApplication) => modelApplication
            .ToReactiveModule<IModelReactiveModulesPositionInListView>().PositionInListView;
    }
}