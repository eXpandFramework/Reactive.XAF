using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace Xpand.XAF.Modules.HideToolBar{
    public interface IModelClassHideToolBar : IModelNode {
        // [Category(HideToolBarModule.CategoryName)]
        bool HideToolBar { get; set; }
    }
    [ModelInterfaceImplementor(typeof(IModelClassHideToolBar), "ModelClass")]
    public interface IModelListViewHideToolBar : IModelClassHideToolBar {
    }
}