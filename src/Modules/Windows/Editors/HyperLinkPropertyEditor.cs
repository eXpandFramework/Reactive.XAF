using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Mask;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;
using ListView = DevExpress.ExpressApp.ListView;

namespace Xpand.XAF.Modules.Windows.Editors {

    public class HyperLinkGridListViewController : ViewController {
        WinColumnsListEditor _gridListEditor;

        public HyperLinkGridListViewController() => TargetViewType = ViewType.ListView;

        protected override void OnViewControlsCreated() {
            base.OnViewControlsCreated();
            _gridListEditor = ((ListView)View).Editor as WinColumnsListEditor;
            var gridView = (GridView)_gridListEditor?.GridView();
            if (gridView != null) gridView.MouseDown += GridView_MouseDown;
        }

        protected override void OnDeactivated() {
            if (_gridListEditor?.GridView() != null)
                _gridListEditor.GridView<GridView>().MouseDown -= GridView_MouseDown;
            base.OnDeactivated();
        }

        void GridView_MouseDown(object sender, MouseEventArgs e) {
            var gv = (GridView)sender;
            GridHitInfo hi = gv.CalcHitInfo(new Point(e.X, e.Y));
            if (hi.InRowCell && hi.Column.ColumnEdit is RepositoryItemHyperLinkEdit repositoryItemHyperLinkEdit) {
                var editor = (HyperLinkEdit)repositoryItemHyperLinkEdit.CreateEditor();
                var memberInfo = View.ToListView().Model.Columns.FirstOrDefault(column => column.FieldName==hi.Column.FieldName)?.ModelMember.MemberInfo;
                editor.ShowBrowser(HyperLinkPropertyEditor.GetResolvedUrl(gv.GetRowCellValue(hi.RowHandle, hi.Column),memberInfo,View.SelectedObjects.Cast<object>().First()));
            }
        }
    }

    [PropertyEditor(typeof(object), EditorAliases.HyperLinkPropertyEditor, false)]
    public class HyperLinkPropertyEditor(Type objectType, IModelMemberViewItem info)
        : StringPropertyEditor(objectType, info), IComplexViewItem {
        public const string UrlEmailMask = @"(((http|https|ftp)\://)?[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*)|([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,100})";

        HyperLinkEdit _hyperlinkEdit;
        private IObjectSpace _objectSpace;

        public new HyperLinkEdit Control => _hyperlinkEdit;

        protected override RepositoryItem CreateRepositoryItem() => new RepositoryItemHyperLinkEdit();

        protected override object CreateControlCore() => _hyperlinkEdit = new HyperLinkEdit();

        protected override void SetupRepositoryItem(RepositoryItem item) {
            base.SetupRepositoryItem(item);
            var hyperLinkProperties = (RepositoryItemHyperLinkEdit)item;
            hyperLinkProperties.SingleClick = View is ListView;
            hyperLinkProperties.TextEditStyle = TextEditStyles.Standard;
            hyperLinkProperties.OpenLink += hyperLinkProperties_OpenLink;
            EditMaskType = EditMaskType.RegEx;
            hyperLinkProperties.Mask.MaskType = MaskType.RegEx;
            hyperLinkProperties.Mask.EditMask = UrlEmailMask;
        }

        void hyperLinkProperties_OpenLink(object sender, OpenLinkEventArgs e) 
            => e.EditValue = GetResolvedUrl(e.EditValue,MemberInfo, CurrentObject);

        public override void BreakLinksToControl(bool unwireEventsOnly){
            base.BreakLinksToControl(unwireEventsOnly);
            _objectSpace.Committing-=ObjectSpaceOnCommitting;
        }

        public static string GetResolvedUrl(object value, IMemberInfo memberInfo,object currentObject) {
            var editorAttribute = memberInfo.FindAttribute<HyperLinkPropertyEditorAttribute>();
            if (editorAttribute != null) {
                value = memberInfo.Owner.FindMember(editorAttribute.Name).GetValue(currentObject);
            }
            string url = Convert.ToString(value);
            if (!string.IsNullOrEmpty(url)) {
                if (url.Contains("@") && IsValidUrl(url))
                    return $"mailto:{url}";
                if (!url.Contains("://"))
                    url = $"http://{url}";
                if (IsValidUrl(url))
                    return url;
            }
            return string.Empty;
        }

        static bool IsValidUrl(string url) => Regex.IsMatch(url, UrlEmailMask);

        public void Setup(IObjectSpace objectSpace, XafApplication application){
            _objectSpace = objectSpace;
            objectSpace.Committing+=ObjectSpaceOnCommitting;
        }

        private void ObjectSpaceOnCommitting(object sender, CancelEventArgs cancelEventArgs){
            if (_hyperlinkEdit?.MaskBox != null) cancelEventArgs.Cancel = !_hyperlinkEdit.MaskBox.IsMatch;
        }
    }
}
