using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace Xpand.XAF.Modules.SuppressConfirmation{
    public interface IModelClassSupressConfirmation : IModelNode {
        [Category(SuppressConfirmationModule.CategoryName)]
        bool SupressConfirmation { get; set; }
    }
    [ModelInterfaceImplementor(typeof(IModelClassSupressConfirmation), "ModelClass")]
    public interface IModelObjectViewSupressConfirmation : IModelClassSupressConfirmation {
    }
}