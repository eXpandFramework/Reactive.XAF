using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.Data;
using DevExpress.ExpressApp.DC;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.GridListEditor.Appearance;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;


namespace Xpand.XAF.Modules.GridListEditor{
    using DevExpress.ExpressApp;
    using DevExpress.ExpressApp.Win.Editors;
    using DevExpress.XtraGrid.Views.Grid;
    using DevExpress.Utils;

    public static class GridListEditorService {
        internal static IObservable<Unit> Connect(this  ApplicationModulesManager manager) 
            => manager.WhenApplication(application => 
                application.RememberTopRow()
                    .Merge(application.FocusRow())
                    .Merge(application.HideIndicatorRow())
                    .Merge(application.SortProperties())
                    .Merge(application.ColSummaryDisplay())
                    .Merge(application.SortPartialGroups())
                    .Merge(application.AppearanceTooltip())
                    .Merge(application.PreviewAppearance())
            )
            .MergeToUnit(manager.SortProperties());

        private static IObservable<(Frame frame,TRule rule)> WhenRulesOnView<TRule>(this XafApplication application) where TRule:IModelGridListEditorRule
            => application.WhenViewOnFrame(viewType: ViewType.ListView)
                .SelectMany(frame => application.ModelRules<TRule>(frame).Select(rule => (frame,rule)))
                .SelectMany(t => t.frame.View.WhenControlsCreated(true).To(t));

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
                    .SelectManyItemResilient(view => view.SortPartialGroups())
                )
                .ToUnit();

        private static IObservable<Unit> SortPartialGroups(this ListView listView) {
            var gridView = listView.Editor.GridView<GridView>();
            gridView.OptionsBehavior.AllowPartialGroups = DefaultBoolean.True;
            gridView.Columns.Where(x => x.GroupIndex >= 0).Do(x => x.SortMode = ColumnSortMode.Custom).Enumerate();
            return gridView.ProcessEvent<CustomColumnSortEventArgs>(nameof(gridView.CustomColumnSort)).Do(e => {
                if (e.Column.GroupIndex < 0) return;
                var objs = gridView.Objects();
                e.Column.SortOrder = ColumnSortOrder.Ascending;
                var mem = e.Column.MemberInfo();
                var cnts = objs.GroupBy(x => mem.GetValue(x)??"").ToDictionary(x => x.Key, x => x.Count());
                if (!cnts.Any()) return;
                int c1 = 0;
                if (e.Value1 != null) cnts.TryGetValue(e.Value1, out c1);
                int c2 = 0;
                if (e.Value2 != null) cnts.TryGetValue(e.Value2, out c2);
                if (c1 == 1 && c2 == 1) e.Result = 0;
                else if (c1 > 1 && c2 > 1) e.Result = Comparer.Default.Compare(e.Value1, e.Value2);
                else e.Result = c1.CompareTo(c2);
                e.Handled = true;
            }).ToUnit();
        }
        
        static IMemberInfo MemberInfo(this GridColumn column) {
            var propertyDescriptor = column.View.DataController.Columns[column.ColumnHandle]?.PropertyDescriptor as XafPropertyDescriptor;
            return propertyDescriptor?.MemberInfo;
        }

        public static List<object> Objects(this GridView gridView){
            var objects=new List<object>();
            for (int i = 0; i < gridView.DataRowCount; i++) {
                objects.Add(gridView.GetRow(i));
            }
            return objects;
        }

        static IObservable<Unit> ColSummaryDisplay(this XafApplication application)
            => application.WhenSetupComplete().SelectMany(_ => application.WhenFrame(ViewType.ListView).ToListView()
                .WhenControlsCreated(true)
                .Select(listView => listView.Editor).OfType<GridListEditor>()
                .SelectMany(editor => editor.GridView.DataSource.Observe().WhenNotDefault()
                    .SwitchIfEmpty(editor.GridView.ProcessEvent(nameof(editor.GridView.DataSourceChanged)).Take(1))
                    .SelectMany(_ => editor.GridView.Columns.Where(column => column.Visible).ToNowObservable()
                        .SelectManyItemResilient(column => {
                            var memberInfo = column.MemberInfo();
                            if (memberInfo==null)return Observable.Empty<Unit>();
                            var columnSummaryAttribute = memberInfo.FindAttribute<ColumnSummaryAttribute>();
                            return columnSummaryAttribute?.HideCaption??false ? column.Summary
                                    .Do(item => item.DisplayFormat = item.DisplayFormat.Replace("SUM=", ""))
                                    .ToNowObservable().ToUnit() : Observable.Empty<Unit>();
                        }))));

        static IObservable<Unit> SortProperties(this XafApplication application) 
            => application.WhenSetupComplete().SelectMany(_ => application.WhenFrame(ViewType.ListView).ToListView().WhenControlsCreated(true)
                .SelectMany(listView => listView.Model.Columns.WhereNotDefault(column => column.ModelMember)
                    .Select(column => (attribute: column.ModelMember.MemberInfo.FindAttribute<SortPropertyAttribute>(), column))
                    .WhereNotDefault(t => t.attribute)
                    .Do(t => {
                        var xafGridColumnWrappers = ((WinColumnsListEditor)listView.Editor).Columns.OfType<WinGridColumnWrapper>();
                        var columnWrapper = xafGridColumnWrappers.FirstOrDefault(wrapper => t.column.Id == wrapper.Id);
                        if (columnWrapper == null) return;
                        columnWrapper.Column.OptionsColumn.AllowSort = DefaultBoolean.True;
                        columnWrapper.Column.FieldNameSortGroup = listView.ObjectTypeInfo.FindMember(columnWrapper.PropertyName) is MemberPathInfo memberInfo
                            ? $"{memberInfo.GetPath().SkipLast(1).Select(info => info.Name).JoinComma()}.{t.attribute.Name}" : t.attribute.Name;
                    }))
                .ToUnit());

        static IObservable<Unit> FocusRow(this XafApplication application)
            => application.WhenRulesOnView<IModelGridListEditorFocusRow>()
                .SelectManyItemResilient(t => {
                    var gridView = t.frame.View.AsListView().GridView();
                    return t.rule.FocusRow(gridView).Merge(t.rule.MoveFocus( gridView));
                })
                .ToUnit();
        static IObservable<Unit> HideIndicatorRow(this XafApplication application)
            => application.WhenRulesOnView<IModelGridListEditorHideIndicatorRow>()
                .Do(t => t.frame.View.AsListView().GridView().OptionsView.ShowIndicator=false)
                .ToUnit();

        private static IObservable<Unit> FocusRow(this IModelGridListEditorFocusRow rule, GridView gridView) 
            => AppDomain.CurrentDomain.GridControlHandles().Where(info => info.Name == rule.RowHandle)
                .Select(info => info.GetValue(null))
                .Do(o => gridView.FocusedRowHandle=(int)o)
                .ToObservable().ToUnit();

        private static IObservable<Unit> MoveFocus(this IModelGridListEditorFocusRow rule, GridView gridView) 
            => gridView.ProcessEvent<EventArgs>("KeyDown").Where(eventArgs =>gridView.FocusedRowHandle==0&& eventArgs.GetPropertyValue("KeyCode").ToString()=="Up")
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
                .SelectMany(frame => {
                    var gridView = ((GridListEditor)frame.View.ToListView().Editor).GridView;
                    return gridView.ProcessEvent<EventArgs>(nameof(DevExpress.XtraGrid.Views.Grid.GridView.CustomDrawCell))
                        .Select(eventArgs => (e: (RowCellCustomDrawEventArgs)eventArgs, gridView, frame));
                });

        public static IObservable<(RowCellCustomDrawEventArgs e, GridView gridView, Frame frame)> WhenCustomDrawGridViewCell(this XafApplication application, Type objectType = null,
                Nesting nesting = Nesting.Any, params Type[] columnsTypes) 
            => application.WhenCustomDrawGridViewCell( objectType, nesting).Where(t => columnsTypes.Length==0||columnsTypes.Contains(t.e.Column.ColumnType));

        public static T GetObject<T>(this CollectionSourceBase collectionSource,GridView gridView,int rowHandle) 
            => (T)DevExpress.ExpressApp.Win.Core.XtraGridUtils.GetRow(collectionSource, gridView, rowHandle);
        
        private static GridView GridView(this ListView view) => ((GridListEditor)view.Editor).GridView;

        private static IObservable<TRule> ModelRules<TRule>(this XafApplication application, Frame frame) where TRule:IModelGridListEditorRule
            => application.ReactiveModulesModel().GridListEditor().Rules().OfType<TRule>()
		        .Where(row =>row.ListView == frame.View.Model &&((ListView) frame.View).Editor is GridListEditor )
		        .TraceGridListEditor(row => row.ListView.Id);

        internal static IObservable<TSource> TraceGridListEditor<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, GridListEditorModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);

        public static IObservable<(TRepositoryItem edit, CancelEventArgs showBrowserrArgs, CustomRowCellEditEventArgs cellEditArgs)> WhenRepositoryItems<TRepositoryItem>(this Frame frame,
            Func<BaseEdit,IObservable<CancelEventArgs>> customize) where TRepositoryItem:RepositoryItem
            => frame.View.WhenControlsCreated(true).SelectMany(_ => frame.View.ToListView().Editor.GridView<GridView>().GridControl.RepositoryItems
                .Cast<RepositoryItem>().OfType<TRepositoryItem>().ToNowObservable()
                .SelectMany(edit => (customize(edit.CreateEditor())).Select(e => (edit,e)))
                .CombineLatestWhenFirstEmits(frame.View.ToListView().Editor.GridView<GridView>()
                    .ProcessEvent<CustomRowCellEditEventArgs>(nameof(DevExpress.XtraGrid.Views.Grid.GridView.CustomRowCellEdit)),(t, e) =>(t.edit,showBrowserrArgs:t.e,cellEditArgs:e) )
                .Where(t => t.edit==t.cellEditArgs.RepositoryItem));
    }
}