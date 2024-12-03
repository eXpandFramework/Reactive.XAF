using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Mask;
using DevExpress.XtraEditors.Repository;
using Xpand.Extensions.XAF.Attributes;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;
using ListView = DevExpress.ExpressApp.ListView;

namespace Xpand.XAF.Modules.Windows.Editors {


    [PropertyEditor(typeof(object), EditorAliases.HyperLinkPropertyEditor, false)]
    public class HyperLinkPropertyEditor(Type objectType, IModelMemberViewItem info)
        : StringPropertyEditor(objectType, info), IComplexViewItem {
        public const string UrlEmailMask = @"(((http|https|ftp)\://)?[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*)|([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,100})";

        HyperLinkEdit _hyperlinkEdit;
        private IObjectSpace _objectSpace;
        
        
        public new HyperLinkEdit Control => _hyperlinkEdit;

        protected override RepositoryItem CreateRepositoryItem() => new RepositoryItemHyperLinkEdit();

        protected override object CreateControlCore() {
            var hyperLinkEdit = _hyperlinkEdit = new HyperLinkEdit();
            hyperLinkEdit.Validating+=HyperLinkEditOnValidating;
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
