using System;
using System.Windows.Forms;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;

namespace Xpand.XAF.Modules.Windows.Editors;
[PropertyEditor(typeof(object),EditorAliases.LabelPropertyEditor,false)]
public class LabelControlPropertyEditor : StringPropertyEditor{
    static LabelControlPropertyEditor() => EnableDisplayFormat = true;

    public LabelControlPropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) 
        => AllowEdit[nameof(LabelControlPropertyEditor)] = false;
    
    protected override object CreateControlCore() {
        var controlCore = base.CreateControlCore();
        if (controlCore is LargeStringEdit edit) {
            edit.Properties.ScrollBars=ScrollBars.None;
        }

        var textEdit = ((TextEdit)controlCore);
        textEdit.Enabled=false;
        textEdit.ReadOnly=false;
        textEdit.BorderStyle=BorderStyles.NoBorder;
        textEdit.Properties.AllowFocused=false;
        textEdit.AutoSize = true;
        textEdit.AutoSizeInLayoutControl = true;
        return controlCore;
    }

    protected override void SetupRepositoryItem(RepositoryItem item) {
        base.SetupRepositoryItem(item);
        item.BorderStyle = BorderStyles.NoBorder;
    }

    
}