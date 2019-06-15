using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.Source.Extensions.XAF.Model;

namespace Xpand.XAF.Modules.ModelMapper{
    [DomainLogic(typeof(IModelNodeDisabled))]
    public class ModelNodeEnabledDomainLogic{
        public static IModelObjectView Get_ParentObjectView(IModelNodeDisabled modelNodeDisabled){
            return modelNodeDisabled.GetParent<IModelObjectView>();
        }
    }

    public interface IModelNodeDisabled : IModelNode {
        [DefaultValue(true)]
        [Category("Activation")]
        bool NodeDisabled { get; set; }
        [Browsable(false)]
        IModelObjectView ParentObjectView { get; }

    }
}