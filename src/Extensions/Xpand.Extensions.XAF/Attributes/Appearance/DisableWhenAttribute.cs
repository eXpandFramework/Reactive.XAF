using DevExpress.ExpressApp.ConditionalAppearance;

namespace Xpand.Extensions.XAF.Attributes.Appearance{
    
    public class DisableWhenAttribute:AppearanceAttribute {
        public DisableWhenAttribute(string criteria):base($"{nameof(DisableWhenAttribute)}_{criteria}",DevExpress.ExpressApp.ConditionalAppearance.AppearanceItemType.ViewItem, criteria) 
            => Enabled = false;
    }
}