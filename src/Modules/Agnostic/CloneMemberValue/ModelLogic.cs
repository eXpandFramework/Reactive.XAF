using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace Xpand.XAF.Modules.CloneMemberValue{
    public interface IModelMemberCloneValue : IModelNode {
        [Category("eXpand")]
        bool CloneValue { get; set; }
    }
    [ModelInterfaceImplementor(typeof(IModelMemberCloneValue), "ModelMember")]
    public interface IModelCommonMemberViewItemCloneValue : IModelMemberCloneValue {

    }
}