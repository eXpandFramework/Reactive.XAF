using System;
using System.Collections;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.Data;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.Utils;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.GridListEditor{
    public static class GridListEditorService {
        
        internal static IObservable<Unit> Connect(this  ApplicationModulesManager manager) 
            => manager.WhenApplication(application => 
                application.RememberTopRow()
                    .Merge(application.FocusRow())
                    .Merge(application.SortProperties())
                    .Merge(application.ColSummaryDisplay())
                    .Merge(application.SortPartialGroups())
            )
            .MergeToUnit(manager.SortProperties());

        private static IObservable<(Frame frame,TRule rule)> WhenRulesOnView<TRule>(this XafApplication application) where TRule:IModelGridListEditorRule
            => application.WhenViewOnFrame(viewType: ViewType.ListView)
                .SelectMany(frame => application.ModelRules<TRule>(frame).Select(rule => (frame,rule)));

        private static IObservable<Unit> SortProperties(this ApplicationModulesManager manager) 
            => manager.WhenCustomizeTypesInfo().SelectMany(e => e.TypesInfo.PersistentTypes.Attributed<SortPropertyAttribute>())
                .SelectMany(t => t.typeInfo.Members.Where(info =>info.IsPublic&& info.FindAttribute<SortPropertyAttribute>()==null)
                    .Do(info => info.AddAttribute(t.attribute)))
                .ToUnit();

        static IObservable<Unit> SortPartialGroups(this XafApplication application)
            => application.WhenSetupComplete()
                .SelectMany(_ => application.WhenFrame(ViewType.ListView).ToListView()
                    .Where(view => view.ObjectTypeInfo.FindAttributes<PartialGroupsAttribute>().Any())
                    .WhenControlsCreated(true)
                    .SelectMany(SortPartialGroups)
                )
                .ToUnit();

        private static IObservable<Unit> SortPartialGroups(this ListView listView) {
            var gridView = listView.Editor.GridView<GridView>();
            gridView.OptionsBehavior.AllowPartialGroups=DefaultBoolean.True;
            gridView.Columns.Where(column => column.GroupIndex>=0).Do(column => column.SortMode=ColumnSortMode.Custom).Enumerate();
            var objects = listView.Objects().ToArray();
            return gridView.WhenEvent<CustomColumnSortEventArgs>(nameof(gridView.CustomColumnSort))
                .Do(e => {
                    if (e.Column.GroupIndex<0) return;
                    e.Column.OptionsColumn.AllowSort = DefaultBoolean.False;
                    e.Column.SortOrder=ColumnSortOrder.Ascending;
                    var memberInfo = e.Column.MemberInfo();
                    var counts = objects.GroupBy(x=>memberInfo.GetValue(x))
                        .ToDictionary(x=>x.Key, x=>x.Count());
                    var countValue1 = counts[e.Value1];
                    var countValue2 = counts[e.Value2];
                    e.Result = countValue1 == countValue2 ? countValue1 == 1 ? Comparer.Default.Compare(e.Value1, e.Value2) : 0 : -1;
                    e.Handled = true;

                }).ToUnit();
        }

        static IObservable<Unit> ColSummaryDisplay(this XafApplication application)
            => application.WhenSetupComplete().SelectMany(_ => application.WhenFrame(ViewType.ListView).ToListView()
                .WhenControlsCreated(true)
                .SelectMany(listView => {
                    var gridView = listView.Editor.GridView<GridView>();
                    if (gridView == null)return Observable.Empty<Unit>();
                    return gridView.DataSource.Observe().WhenNotDefault()
                        .SwitchIfEmpty(gridView.WhenEvent(nameof(gridView.DataSourceChanged)).Take(1))
                        .SelectMany(_ => gridView.Columns.Where(column => column.Visible).ToNowObservable().SelectMany(column => {
                            var memberInfo = column.MemberInfo();
                            if (memberInfo==null)return Observable.Empty<Unit>();
                            var columnSummaryAttribute = memberInfo.FindAttribute<ColumnSummaryAttribute>();
                            return columnSummaryAttribute?.HideCaption??false ? column.Summary
                                    .Do(item => item.DisplayFormat = item.DisplayFormat.Replace("SUM=", ""))
                                    .ToNowObservable().ToUnit()
                                : Observable.Empty<Unit>();
                        }));
                }));

        public static IMemberInfo MemberInfo(this GridColumn column) 
            => ((XafPropertyDescriptor)column.View.DataController.Columns[column.ColumnHandle]?.PropertyDescriptor)?.MemberInfo;

        static IObservable<Unit> SortProperties(this XafApplication application) 
            => application.WhenSetupComplete().SelectMany(_ => application.WhenFrame(ViewType.ListView).ToListView().WhenControlsCreated(true)
                .SelectMany(listView => listView.Model.Columns.Select(column => (attribute: column.ModelMember.MemberInfo.FindAttribute<SortPropertyAttribute>(), column))
                    .WhereNotDefault(t => t.attribute)
                    .Do(t => {
                        var xafGridColumnWrappers = ((WinColumnsListEditor)listView.Editor).Columns.OfType<XafGridColumnWrapper>();
                        var columnWrapper = xafGridColumnWrappers.FirstOrDefault(wrapper => t.column.Id == wrapper.Id);
                        if (columnWrapper == null) return;
                        columnWrapper.Column.OptionsColumn.AllowSort = DefaultBoolean.True;
                        columnWrapper.Column.FieldNameSortGroup = listView.ObjectTypeInfo.FindMember(columnWrapper.PropertyName) is MemberPathInfo memberInfo
                            ? $"{memberInfo.GetPath().SkipLast(1).Select(info => info.Name).JoinComma()}.{t.attribute.Name}" : t.attribute.Name;
                    }))
                .ToUnit());

        static IObservable<Unit> FocusRow(this XafApplication application)
            => application.WhenRulesOnView<IModelGridListEditorFocusRow>()
                .SelectMany(t => {
                    var gridView = t.frame.View.AsListView().GridView();
                    return t.rule.FocusRow(gridView).Merge(t.rule.MoveFocus( gridView));
                })
                .ToUnit();

        private static IObservable<Unit> FocusRow(this IModelGridListEditorFocusRow rule, GridView gridView) 
            => AppDomain.CurrentDomain.GridControlHandles().Where(info => info.Name == rule.RowHandle)
                .Select(info => info.GetValue(null))
                .Do(o => gridView.FocusedRowHandle=(int)o)
                .ToObservable().ToUnit();

        private static IObservable<Unit> MoveFocus(this IModelGridListEditorFocusRow rule, GridView gridView) 
            => gridView.WhenEvent("KeyDown").Where(p =>gridView.FocusedRowHandle==0&& p.EventArgs.GetPropertyValue("KeyCode").ToString()=="Up")
                .SelectMany(_ => AppDomain.CurrentDomain.GridControlHandles().Where(info => info.Name == rule.UpArrowMoveToRowHandle)
                    .Select(info => info.GetValue(null))
                    .Do(o => gridView.FocusedRowHandle=(int)o)).ToUnit();

        static IObservable<Unit> RememberTopRow(this XafApplication application) 
            => application.WhenRulesOnView<IModelGridListEditorTopRow>().Select(t => t.frame).MergeIgnored(frame => {
                var view = frame.View.AsListView();
                var gridView = view.GridView();
                var topRowIndex = gridView.TopRowIndex;
                return view.CollectionSource.WhenCollectionReloaded()
                    .Do(_ => gridView.TopRowIndex=topRowIndex)
                    .To($"TopRowIndex: {topRowIndex}, View: {view}")
                    .TraceGridListEditor();
            }).ToUnit();

        public static IObservable<(RowCellCustomDrawEventArgs e, GridView gridView, Frame frame)> WhenCustomDrawGridViewCell(this XafApplication application, Type objectType = null,
                Nesting nesting = Nesting.Any, params string[] columnsNames) 
            => application.WhenCustomDrawGridViewCell( objectType, nesting).Where(t => columnsNames.Length==0||columnsNames.Contains(t.e.Column.Name));

        public static IObservable<(RowCellCustomDrawEventArgs e, GridView gridView, Frame frame)> WhenCustomDrawGridViewCell(this XafApplication application, Type objectType, Nesting nesting) 
            => application.WhenFrame(objectType,ViewType.ListView,nesting)
                .SelectMany(frame => frame.View.WhenControlsCreated(true).To(frame))
                .SelectMany(frame => ((DevExpress.ExpressApp.Win.Editors.GridListEditor)frame.View.ToListView().Editor).GridView
                    .WhenEvent(nameof(DevExpress.XtraGrid.Views.Grid.GridView.CustomDrawCell))
                    .Select(pattern => (e:(RowCellCustomDrawEventArgs)pattern.EventArgs,gridView:(GridView)pattern.Sender,frame)));

        public static IObservable<(RowCellCustomDrawEventArgs e, GridView gridView, Frame frame)> WhenCustomDrawGridViewCell(this XafApplication application, Type objectType = null,
                Nesting nesting = Nesting.Any, params Type[] columnsTypes) 
            => application.WhenCustomDrawGridViewCell( objectType, nesting).Where(t => columnsTypes.Length==0||columnsTypes.Contains(t.e.Column.ColumnType));

        public static T GetObject<T>(this CollectionSourceBase collectionSource,GridView gridView,int rowHandle) 
            => (T)DevExpress.ExpressApp.Win.Core.XtraGridUtils.GetRow(collectionSource, gridView, rowHandle);
        
        private static GridView GridView(this ListView view) => ((DevExpress.ExpressApp.Win.Editors.GridListEditor)view.Editor).GridView;

        private static IObservable<TRule> ModelRules<TRule>(this XafApplication application, Frame frame) where TRule:IModelGridListEditorRule
            => application.ReactiveModulesModel().GridListEditor().Rules().OfType<TRule>()
		        .Where(row =>row.ListView == frame.View.Model &&((ListView) frame.View).Editor is DevExpress.ExpressApp.Win.Editors.GridListEditor )
		        .TraceGridListEditor(row => row.ListView.Id);

        internal static IObservable<TSource> TraceGridListEditor<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, GridListEditorModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);
    }
}