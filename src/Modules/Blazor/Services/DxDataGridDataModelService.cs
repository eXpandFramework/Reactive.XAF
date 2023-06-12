using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Grid;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using Fasterflect;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Blazor.Model;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Blazor.Services {
    public static class DxDataGridDataModelService {
        internal static IObservable<Unit> ApplyDxDataGridModel(this XafApplication application) 
            => application.WhenFrameViewControls()
                .WhenFrame(viewType: ViewType.ListView)
                // .MergeIgnored(frame => frame.GridModel().Apply())
                .MergeIgnored(frame => frame.ColumnModel().Apply())
                .ToUnit();

        private static IObservable<(Frame frame, IModelListViewFeatureDxDataGridColumnModel model)> ColumnModel(this Frame frame) 
            =>frame.Application.Model.ToReactiveModule<IModelReactiveModulesBlazor>().Blazor.ListViewFeatures.OfType<IModelListViewFeatureDxDataGridColumnModel>()
                .SelectMany(model => model.ListViewColumns.Select(column => (column,model)).Where(t => t.column.ListView==frame.View.Model))
                .Select(t => (frame,t.model)).ToObservable();

        private static IObservable<(Frame frame, IModelListViewFeatureDxDataGridModel model)> GridModel(this Frame frame) 
            => frame.Application.Model.ToReactiveModule<IModelReactiveModulesBlazor>().Blazor.ListViewFeatures.OfType<IModelListViewFeatureDxDataGridModel>()
                .SelectMany(model => model.ListViews.Select(key => (key,model))).Where(t => t.key.ListView==frame.View.Model)
                .Select(t => (frame,t.model)).ToObservable();

        private static IObservable<Unit> Apply(this IObservable<(Frame frame, IModelListViewFeatureDxDataGridColumnModel model)> source) 
            => source.SelectMany(t => ColumnModelProperties
                    .Select(s => (name:s,value:t.model.GetValue(s))).Where(t3 => t3.value!=null)
                    .Select(t1 =>( t1.name,t1.value,t.frame))
                    .SelectMany(t2 => ((DxGridListEditor) t.frame.View.AsListView().Editor).Columns.Cast<DxGridColumnWrapper>()
                        .Select(wrapper => wrapper.GetPropertyValue("DxGridDataColumnModel")).Cast<DxGridDataColumnModel>()
                        .Do(model => model.SetPropertyValue(t2.name, t2.value))))
                .ToUnit();

        private static IObservable<Unit> Apply(this IObservable<(Frame frame, IModelListViewFeatureDxDataGridModel model)> source) 
            => source.SelectMany(t => GridModelProperties
                    .Select(s => (name:s,value:t.model.GetValue(s))).Where(t3 => t3.value!=null)
                    .Select(t1 =>( t1.name,t1.value,t.frame))
                    .Do(t2 => ((DxGridListEditor) t.frame.View.AsListView().Editor).GetGridAdapter()
                        .GridModel.SetPropertyValue(t2.name, t2.value)))
                .ToUnit();

        private static string[] GridModelProperties { get; } = typeof(IModelListViewFeatureDxDataGridModel).GetProperties()
            .Select(info => info.Name).Where(s => s != nameof(IModelListViewFeatureDxDataGridModel.ListViews)).ToArray();
        
        private static string[] ColumnModelProperties { get; } = typeof(IDxDataGridColumnModel).GetProperties()
            .Select(info => info.Name).Where(s => s != nameof(IModelListViewFeatureDxDataGridColumnModel.ListViewColumns)).ToArray();

    }
}