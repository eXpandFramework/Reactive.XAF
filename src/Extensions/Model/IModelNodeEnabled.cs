using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;

namespace DevExpress.XAF.Extensions.Model{
    public interface IModelNodeEnabled : IModelNode {
        [DefaultValue(true)]
        [Category("Activation")]
        bool NodeEnabled { get; set; }
        [Browsable(false)]
        IModelObjectView ParentObjectView { get; }

    }
    [DomainLogic(typeof(IModelNodeEnabled))]
    public class ModelNodeEnabledDomainLogic{
        public static IModelObjectView Get_ParentObjectView(IModelNodeEnabled modelNodeEnabled){
            return modelNodeEnabled.GetParent<IModelObjectView>();
        }
    }

}