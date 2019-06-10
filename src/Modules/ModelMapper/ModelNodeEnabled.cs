using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.Source.Extensions.XAF.Model;

namespace Xpand.XAF.Modules.ModelMapper{
    [DomainLogic(typeof(IModelNodeEnabled))]
    public class ModelNodeEnabledDomainLogic{
        public static IModelObjectView Get_ParentObjectView(IModelNodeEnabled modelNodeEnabled){
            return modelNodeEnabled.GetParent<IModelObjectView>();
        }
    }

    public interface IModelNodeEnabled : IModelNode {
        [DefaultValue(true)]
        [Category("Activation")]
        bool NodeEnabled { get; set; }
        [Browsable(false)]
        IModelObjectView ParentObjectView { get; }

    }
}