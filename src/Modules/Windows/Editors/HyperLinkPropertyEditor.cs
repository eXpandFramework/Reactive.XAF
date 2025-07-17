using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Mask;
using DevExpress.XtraEditors.Registrator;
using DevExpress.XtraEditors.Repository;
using Xpand.Extensions.XAF.Attributes;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;
using ListView = DevExpress.ExpressApp.ListView;

namespace Xpand.XAF.Modules.Windows.Editors {


    [PropertyEditor(typeof(object), EditorAliases.HyperLinkPropertyEditor, false)]
    public class HyperLinkPropertyEditor(Type objectType, IModelMemberViewItem info)
        : StringPropertyEditor(objectType, info), IComplexViewItem {
        public const string UrlEmailMask = @"(((http|https|ftp)\://)?[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*)|([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,100})";

        MyHyperLinkEdit _hyperlinkEdit;
        private IObjectSpace _objectSpace;
        
        
        public new MyHyperLinkEdit Control => _hyperlinkEdit;

        protected override RepositoryItem CreateRepositoryItem() => new RepositoryItemHyperLinkEdit();

        protected override object CreateControlCore() {
            var hyperLinkEdit = _hyperlinkEdit = new MyHyperLinkEdit();
            hyperLinkEdit.Validating+=HyperLinkEditOnValidating;
            if (!Model.AllowEdit||!Model.ParentView.AllowEdit) {
                hyperLinkEdit.BorderStyle=BorderStyles.NoBorder;
                hyperLinkEdit.Properties.BorderStyle=BorderStyles.NoBorder;
            }
            return hyperLinkEdit;
        }

        private void HyperLinkEditOnValidating(object sender, CancelEventArgs e) {
            var attribute = MemberInfo.FindAttribute<HyperLinkPropertyEditorAttribute>();
            if (attribute==null) return;
            var value = MemberInfo.Owner.FindMember(attribute.Name).GetValue(CurrentObject);
            e.Cancel = value != null && !Regex.IsMatch($"{value}", UrlEmailMask);
        }
        
        protected override void SetupRepositoryItem(RepositoryItem item) {
            base.SetupRepositoryItem(item);
            var hyperLinkProperties = (DevExpress.XtraEditors.Repository.RepositoryItemHyperLinkEdit)item;
            hyperLinkProperties.SingleClick = View is ListView;
            hyperLinkProperties.TextEditStyle = TextEditStyles.Standard;
            hyperLinkProperties.OpenLink += hyperLinkProperties_OpenLink;
            EditMaskType = EditMaskType.RegEx;
            hyperLinkProperties.Mask.MaskType = MaskType.RegEx;
            hyperLinkProperties.Mask.EditMask = UrlEmailMask;
            if (!Model.AllowEdit||!Model.ParentView.AllowEdit) hyperLinkProperties.BorderStyle=BorderStyles.NoBorder;
        }

        void hyperLinkProperties_OpenLink(object sender, OpenLinkEventArgs e) {
            e.EditValue = GetResolvedUrl(e.EditValue, MemberInfo, CurrentObject);
            e.Handled = false;
        }

        public override void BreakLinksToControl(bool unwireEventsOnly){
            base.BreakLinksToControl(unwireEventsOnly);
            _objectSpace.Committing-=ObjectSpaceOnCommitting;
        }

        public static string GetResolvedUrl(object value, IMemberInfo memberInfo,object currentObject) {
            var editorAttribute = memberInfo.FindAttribute<HyperLinkPropertyEditorAttribute>();
            if (editorAttribute != null) {
                value = memberInfo.LastMember.Owner.FindMember(editorAttribute.Name).GetValue(currentObject);
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

        private void ObjectSpaceOnCommitting(object sender, CancelEventArgs cancelEventArgs) {
            if (_hyperlinkEdit?.MaskBox == null || MemberInfo.FindAttribute<HyperLinkPropertyEditorAttribute>() != null) return;
            cancelEventArgs.Cancel = !_hyperlinkEdit.MaskBox.IsMatch;
        }

    }

    public class RepositoryItemHyperLinkEdit:DevExpress.XtraEditors.Repository.RepositoryItemHyperLinkEdit {
        private MyHyperLinkEdit _myHyperLinkEdit;

        public static void Register() {
            EditorRegistrationInfo.Default.Editors.Add(new EditorClassInfo(nameof(MyHyperLinkEdit), typeof(MyHyperLinkEdit),  
                typeof(RepositoryItemHyperLinkEdit), typeof(DevExpress.XtraEditors.ViewInfo.HyperLinkEditViewInfo),  
                new DevExpress.XtraEditors.Drawing.HyperLinkEditPainter(), true, null, typeof(DevExpress.Accessibility.TextEditAccessible)));  

        }

        public override BaseEdit CreateEditor() => _myHyperLinkEdit ??= new MyHyperLinkEdit();
    }
    public sealed class MyHyperLinkEdit:HyperLinkEdit {
        static MyHyperLinkEdit() {
            RepositoryItemHyperLinkEdit.Register(); 
        }
        private readonly Subject<CancelEventArgs> _customizeShowBrowserSubject = new();
        private readonly RepositoryItem _item;

        public MyHyperLinkEdit() => AutoSize = true;

        public MyHyperLinkEdit(RepositoryItem item):this() => _item = item;

        protected override RepositoryItem CreateRepositoryItemCore() => _item ?? base.CreateRepositoryItemCore();

        public IObservable<CancelEventArgs> CustomizeShowBrowser => _customizeShowBrowserSubject.AsObservable();
        protected override void Dispose(bool disposing) {
            _customizeShowBrowserSubject.Dispose();
            base.Dispose(disposing);
        }

        public override void ShowBrowser(object linkValue) {
            
            var e = new CancelEventArgs();
            _customizeShowBrowserSubject.OnNext(e);
            if (!e.Cancel) base.ShowBrowser(linkValue);
        }
    }
}
