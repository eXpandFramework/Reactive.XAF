using System.ComponentModel;

namespace Xpand.XAF.Modules.ViewEditMode{
    public interface IModelDetailViewViewEditMode{
        [Category(ViewEditModeModule.CategoryName)]
        DevExpress.ExpressApp.Editors.ViewEditMode? ViewEditMode{ get; set; }
        [DefaultValue(true)]
        [Category(ViewEditModeModule.CategoryName)]
        bool LockViewEditMode{ get; set; }
    }
}