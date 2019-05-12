using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace Xpand.XAF.Modules.AutoCommit{
    public interface IModelClassAutoCommit : IModelNode {
        [Category(AutoCommitModule.CategoryName)]
        bool AutoCommit { get; set; }
    }
    [ModelInterfaceImplementor(typeof(IModelClassAutoCommit), "ModelClass")]
    public interface IModelObjectViewAutoCommit : IModelClassAutoCommit {
    }
}