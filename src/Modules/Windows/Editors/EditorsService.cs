using System;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using Fasterflect;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Windows.Editors{
    static class EditorsService {
        internal static IObservable<Unit> EditorsConnect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenFrame(ViewType.ListView)
                .SelectMany(frame => frame.View.WhenControlsCreated(true)
                    .SelectMany(_ => {
                        var listEditor = frame.View.ToListView().Editor;
                        var gridView = (GridView)listEditor.Control.GetPropertyValue("MainView");
                        return gridView.Observe().WhenNotDefault(view => view.OpenLink(frame)
                            .MergeToUnit(view.HyperLinkPropertyEditorAttribute(frame)))
                            .MergeToUnit(listEditor.WhenEvent<CustomizeAppearanceEventArgs>(nameof(GridListEditor.CustomizeAppearance))
                                .Do(e => {
                                    var item = e.Item as GridViewRowCellStyleEventArgsAppearanceAdapter;
                                    if (item?.Column.ColumnEdit is not RepositoryItemHyperLinkEdit repositoryItem) return;
                                    repositoryItem.LinkColor = ((IAppearanceFormat)e.Item).FontColor;
                                }));

                    }))).ToUnit();

        private static IObservable<CustomRowCellEditEventArgs> HyperLinkPropertyEditorAttribute(this GridView gridView, Frame frame) 
            => gridView.WhenEvent<CustomRowCellEditEventArgs>(nameof(gridView.CustomRowCellEdit))
                .Do(e => {
                    if (e.RepositoryItem is not RepositoryItemHyperLinkEdit) return;
                    var memberInfo = frame.View.ObjectTypeInfo.FindMember(e.Column.Name);
                    if (frame.View.ToListView().Model.Columns[memberInfo.Name].PropertyEditorType != typeof(HyperLinkPropertyEditor)) return;
                    var hyperLinkPropertyEditorAttribute = memberInfo.FindAttribute<HyperLinkPropertyEditorAttribute>();
                    if (hyperLinkPropertyEditorAttribute == null) return;
                    if ($"{frame.View.ObjectTypeInfo.FindMember(hyperLinkPropertyEditorAttribute.Name).GetValue(gridView.GetRow(e.RowHandle))}" != string.Empty) return;
                    e.RepositoryItem = new RepositoryItemTextEdit();
                });

        private static IObservable<MouseEventArgs> OpenLink(this GridView gridView, Frame frame)
            => gridView.WhenEvent<MouseEventArgs>(nameof(GridView.MouseDown))
                .Do(e => {
                    var hi = gridView.CalcHitInfo(new Point(e.X, e.Y));
                    if (!hi.InRowCell || hi.Column.ColumnEdit is not RepositoryItemHyperLinkEdit repositoryItemHyperLinkEdit) return;
                    var editor = (HyperLinkEdit)repositoryItemHyperLinkEdit.CreateEditor();
                    var memberInfo = frame.View.ObjectTypeInfo.FindMember(hi.Column.Name);
                    var currentObject = frame.View.SelectedObjects.Cast<object>().FirstOrDefault();
                    if (currentObject==null) return;
                    editor.ShowBrowser(HyperLinkPropertyEditor.GetResolvedUrl(
                        gridView.GetRowCellValue(hi.RowHandle, hi.Column), memberInfo,
                        currentObject));
                });
    }
}