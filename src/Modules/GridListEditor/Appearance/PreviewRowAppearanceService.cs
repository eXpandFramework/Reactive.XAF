using System;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.GridListEditor.Appearance {
    static class PreviewRowAppearanceService {
        internal static IObservable<Unit> PreviewAppearance(this XafApplication application)
            => application.WhenFrame(ViewType.ListView)
                .WhenNotDefault(frame => ((IModelListViewPreviewColumn)frame.View.ToListView().Model).PreviewColumnName)
                .SelectMany(frame => frame.View.WhenControlsCreated(true).To(frame))
                .SelectMany(frame => {
                    var gridListEditor = frame.View.ToListView().Editor as DevExpress.ExpressApp.Win.Editors.GridListEditor;
                    var gridView = gridListEditor?.GridView;
                    if (gridView == null || string.IsNullOrEmpty(gridView.PreviewFieldName)) return Observable.Empty<Unit>();
                    var controller = frame.GetController<AppearanceController>();
                    if (controller == null) return Observable.Empty<Unit>();
                    return PreviewAppearance(gridView, controller, frame);

                })
                .ToUnit();

        private static IObservable<Unit> PreviewAppearance(GridView gridView, AppearanceController controller, Frame frame){
            return gridView.WhenEvent<RowObjectCustomDrawEventArgs>(nameof(gridView.CustomDrawRowPreview))
                .WithLatestFrom(controller.WhenEvent<CollectAppearanceRulesEventArgs>(nameof(AppearanceController.CollectAppearanceRules)),
                    (drawEventArgs, rulesEventArgs) => (drawEventArgs, rulesEventArgs))
                .Do(t => {
                    if (t.drawEventArgs.RowHandle < 0) return;
                    var obj = gridView.GetRow(t.drawEventArgs.RowHandle);
                    var previewProperty = gridView.PreviewFieldName;
                    var rule = t.rulesEventArgs.AppearanceRules.Where(properties
                            => properties.AppearanceItemType == AppearanceItemType.ViewItem.ToString()
                               && properties.TargetItems.Split(';').Any(x => string.Equals(x.Trim(), previewProperty, StringComparison.OrdinalIgnoreCase)))
                        .FirstOrDefault(ruleProperties => frame.View.ObjectSpace.IsObjectFitForCriteria( ruleProperties.Criteria,obj));
                    if (rule != null) {
                        if (rule.FontColor.HasValue)
                            t.drawEventArgs.Appearance.ForeColor = rule.FontColor.Value;
                        if (rule.BackColor.HasValue)
                            t.drawEventArgs.Appearance.BackColor = rule.BackColor.Value;
                        if (rule.FontStyle.HasValue) {
                            var appearanceFont = t.drawEventArgs.Appearance.Font;
                            var ruleFontStyle = rule.FontStyle.Value;
                            t.drawEventArgs.Appearance.Font = new Font(appearanceFont, Enum.Parse<FontStyle>(ruleFontStyle.ToString()));
                        }
                    }

                    t.drawEventArgs.Appearance.DrawBackground(t.drawEventArgs.Cache, t.drawEventArgs.Bounds);
                    t.drawEventArgs.Appearance.DrawString(t.drawEventArgs.Cache,
                        gridView.GetRowPreviewDisplayText(t.drawEventArgs.RowHandle), t.drawEventArgs.Bounds);
                    t.drawEventArgs.Handled = true;
                }).ToFirst().ToUnit();
        }
    }
}
