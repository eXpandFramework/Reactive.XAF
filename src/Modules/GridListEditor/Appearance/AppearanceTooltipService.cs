using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.Utils;
using DevExpress.XtraGrid.Views.Grid;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.GridListEditor.Appearance{
    static class AppearanceTooltipService {
        internal static IObservable<Unit> AppearanceTooltip(this XafApplication application)
            => application.WhenFrameCreated().ToController<AppearanceController>().WhenNotDefault()
                .SelectMany(controller => controller.WhenActiveObjectInfo()
                    .WithLatestFrom(controller.WhenAppearanceApplied().CellAppearance().Cache(),(objectInfoEventArgs, currentCache) => (objectInfoEventArgs, currentCache))
                    .Do(t => {
                        var gridView = controller.View.ToListView().Editor.GridView<GridView>();
                        if (gridView == null) return;
                        var hitInfo = gridView.CalcHitInfo(t.objectInfoEventArgs.ControlMousePosition);
                        if (!hitInfo.InRowCell) return;
                        var key = $"{hitInfo.RowHandle}:{hitInfo.Column.FieldName}";
                        if (!t.currentCache.TryGetValue(key, out var toolTipText) || string.IsNullOrEmpty(toolTipText)) return;
                        t.objectInfoEventArgs.Info = new ToolTipControlInfo(hitInfo.HitTest, toolTipText);
                    }))
                .ToUnit();
    
        private static IObservable<ToolTipControllerGetActiveObjectInfoEventArgs> WhenActiveObjectInfo(this AppearanceController controller) 
            => controller.Frame.WhenViewChanged().Select(t => t.frame.View.AsListView()).WhenNotDefault()
                .SelectMany(view => view.WhenControlsCreated(true).WhenNotDefault(listView => listView.Editor as DevExpress.ExpressApp.Win.Editors.GridListEditor)
                    .SelectMany(listView => listView.WhenActiveObjectInfo()
                        .MergeIgnored(_ => controller.WhenDeactivated().Take(1)
                            .Do(_ => ((DevExpress.ExpressApp.Win.Editors.GridListEditor)listView.Editor).Grid.ToolTipController?.Dispose()))));

        private static IObservable<ToolTipControllerGetActiveObjectInfoEventArgs> WhenActiveObjectInfo(this ListView listView){
            var gridControl = ((DevExpress.ExpressApp.Win.Editors.GridListEditor)listView.Editor).Grid;
            gridControl.ToolTipController=new ToolTipController();
            return gridControl.ToolTipController.WhenEvent<ToolTipControllerGetActiveObjectInfoEventArgs>(nameof(ToolTipController.GetActiveObjectInfo));
        }

        private static IObservable<Dictionary<string, string>> Cache(this IObservable<(AppearanceItemToolTip toolTipItem, GridViewRowCellStyleEventArgsAppearanceAdapter adapter)> source)
            => source.Scan(new Dictionary<string, string>(), (cache, appearance) => {
                cache[$"{appearance.adapter.RowHandle}:{appearance.adapter.Column.FieldName}"] = appearance.toolTipItem.State == AppearanceState.CustomValue
                    ? appearance.toolTipItem.ToolTipText : "";
                return cache;
            });
        
        private static IObservable<(AppearanceItemToolTip toolTipItem, GridViewRowCellStyleEventArgsAppearanceAdapter adapter)> CellAppearance(this IObservable<ApplyAppearanceEventArgs> source)
            => source.Where(e => e.Item is GridViewRowCellStyleEventArgsAppearanceAdapter)
                .Select(e => new {
                    Adapter = e.Item as GridViewRowCellStyleEventArgsAppearanceAdapter,
                    ToolTipItem = e.AppearanceObject.Items.FirstOrDefault(i => i is AppearanceItemToolTip) as AppearanceItemToolTip
                })
                .Where(x => x.ToolTipItem != null && x.ToolTipItem.State != AppearanceState.None)
                .Select(x => (x.ToolTipItem,x.Adapter));
        
        private static IObservable<ApplyAppearanceEventArgs> WhenAppearanceApplied(this AppearanceController controller) 
            => controller.WhenEvent<ApplyAppearanceEventArgs>(nameof(AppearanceController.AppearanceApplied));

    }
}