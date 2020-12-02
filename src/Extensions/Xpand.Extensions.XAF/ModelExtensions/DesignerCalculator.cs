using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public class DesignerCalculator:IModelIsVisible{
        public bool IsVisible(IModelNode node, string propertyName) => DesignerOnlyCalculator.IsRunFromDesigner;
    }
}