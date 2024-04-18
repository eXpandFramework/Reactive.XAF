using DevExpress.ExpressApp.ConditionalAppearance;

namespace Xpand.Extensions.XAF.Attributes{
    
    public class DisableWhenAttribute:AppearanceAttribute {
        public DisableWhenAttribute(string criteria):base($"{nameof(DisableWhenAttribute)}_{criteria}",DevExpress.ExpressApp.ConditionalAppearance.AppearanceItemType.ViewItem, criteria) 
            => Enabled = false;
    }
    
    public class DisableForExistingObject() : DisableWhenAttribute("IsNewObject=false");
}