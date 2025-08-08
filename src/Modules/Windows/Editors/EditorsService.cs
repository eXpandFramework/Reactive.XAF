using System;
using System.ComponentModel;
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
using Xpand.Extensions.DateTimeExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Windows.Editors{
    static class EditorsService {
        internal static IObservable<Unit> EditorsConnect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenFrame(ViewType.ListView)
                .SelectMany(frame => frame.HyperLinkPropertyEditor())
                .MergeToUnit(application.UseHumanizer())).ToUnit();

        private static IObservable<Unit> HyperLinkPropertyEditor(this Frame frame){
            return frame.View.WhenControlsCreated(true)
                .SelectMany(_ => {
                    var listEditor = frame.View.ToListView().Editor as GridListEditor;
                    if (listEditor == null) return Observable.Empty<Unit>();
                    var gridView = (GridView)listEditor.Control.GetPropertyValue("MainView");
                        
                    return gridView.Observe().WhenNotDefault()
                        .SelectMany(view => view.OpenLink(frame)
                            .MergeToUnit(view.HyperLinkPropertyEditorAttribute(frame)))
                        .MergeToUnit(gridView.ProcessEvent<CancelEventArgs>(nameof(gridView.ShowingEditor))
                            .Do(e => {
                                var memberInfo = gridView.FocusedColumn.MemberInfo();
                                if (memberInfo==null)return;
                                var modelColumn = frame.View.ToListView().Model.Columns
                                    .FirstOrDefault(column => column.ModelMember.MemberInfo == memberInfo);
                                if (modelColumn==null)return;
                                e.Cancel = !modelColumn.AllowEdit;
                            }))
                        .MergeToUnit(listEditor.ProcessEvent<CustomizeAppearanceEventArgs>(nameof(GridListEditor.CustomizeAppearance))
                            .Do(e => {
                                var item = e.Item as GridViewRowCellStyleEventArgsAppearanceAdapter;
                                if (item?.Column.ColumnEdit is not DevExpress.XtraEditors.Repository.RepositoryItemHyperLinkEdit repositoryItem) return;
                                repositoryItem.LinkColor = ((IAppearanceFormat)e.Item).FontColor;
                            }));

                });
        }

        public static IObservable<Unit> UseHumanizer(this XafApplication application)
            => application.WhenFrame(ViewType.DetailView)
                .SelectMany(frame => frame.View.WhenControlsCreated(true).To(frame).StartWith(frame))
                .SelectMany(frame => frame.View.ObjectTypeInfo.AttributedMembers<HumanizeAttribute>().ToNowObservable().ToSecond()
                    .SelectMany(memberInfo => frame.View.ToDetailView().GetItems<DXPropertyEditor>().Where(editor => editor.MemberInfo==memberInfo).ToNowObservable()
                        .SelectMany(editor => editor.WhenControlCreated(true))
                        .Select(editor => editor.Control).OfType<TextEdit>()
                        .SelectMany(edit => frame.View.WhenCurrentObjectChanged().To(edit).StartWith(edit))
                        .Do(edit => edit.Text = memberInfo.MemberType.Humanize(memberInfo.GetValue(frame.View.CurrentObject)))
                    )
                )
                .ToUnit();
        
        private static IObservable<CustomRowCellEditEventArgs> HyperLinkPropertyEditorAttribute(this GridView gridView, Frame frame) 
            => gridView.ProcessEvent<CustomRowCellEditEventArgs>(nameof(gridView.CustomRowCellEdit))
                .Do(e => {
                    if (e.RepositoryItem is not DevExpress.XtraEditors.Repository.RepositoryItemHyperLinkEdit) return;
                    var memberInfo = frame.View.ObjectTypeInfo.FindMember(e.Column.Name);
                    if (frame.View.ToListView().Model.Columns[memberInfo.Name].PropertyEditorType != typeof(HyperLinkPropertyEditor)) return;
                    var hyperLinkPropertyEditorAttribute = memberInfo.FindAttribute<HyperLinkPropertyEditorAttribute>();
                    if (hyperLinkPropertyEditorAttribute == null) return;
                    if (memberInfo.GetPath().Count > 1) {
                        var value = memberInfo.ParentOrCurrent().GetValue(gridView.GetRow(e.RowHandle));
                        if ($"{memberInfo.LastMember.Owner.FindMember(hyperLinkPropertyEditorAttribute.Name).GetValue(value)}" != string.Empty) return;
                    }
                    else {
                        if ($"{frame.View.ObjectTypeInfo.FindMember(hyperLinkPropertyEditorAttribute.Name).GetValue(gridView.GetRow(e.RowHandle))}" != string.Empty) return;    
                    }
                    
                    e.RepositoryItem = new RepositoryItemTextEdit();
                });

        private static IObservable<MouseEventArgs> OpenLink(this GridView gridView, Frame frame)
            => gridView.ProcessEvent<MouseEventArgs>(nameof(GridView.MouseDown))
                .Do(e => {
                    var hi = gridView.CalcHitInfo(new Point(e.X, e.Y));
                    if (!hi.InRowCell || hi.Column.ColumnEdit is not DevExpress.XtraEditors.Repository.RepositoryItemHyperLinkEdit repositoryItemHyperLinkEdit) return;
                    var editor = (MyHyperLinkEdit)repositoryItemHyperLinkEdit.CreateEditor();
                    var memberInfo = frame.View.ObjectTypeInfo.FindMember(hi.Column.Name);
                    var currentObject = frame.View.SelectedObjects.Cast<object>().FirstOrDefault();
                    if (currentObject==null) return;
                    var hyperLinkPropertyEditorAttribute = memberInfo.FindAttribute<HyperLinkPropertyEditorAttribute>();
                    if (hyperLinkPropertyEditorAttribute is { ControlClickListView: true } &&
                        !System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) && !System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl)) return;
                    if (memberInfo.GetPath().Count > 1) {
                        currentObject=memberInfo.ParentOrCurrent().GetValue(currentObject);
                        memberInfo = memberInfo.LastMember;
                        
                    }
                    editor.ShowBrowser(Editors.HyperLinkPropertyEditor.GetResolvedUrl(
                        gridView.GetRowCellValue(hi.RowHandle, hi.Column), memberInfo,
                        currentObject));
                });
    }
}