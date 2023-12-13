using DevExpress.ExpressApp.ConditionalAppearance;

namespace Xpand.Extensions.XAF.Attributes{
    public class DisableForExistingObject:AppearanceAttribute {
        public DisableForExistingObject():base(nameof(DisableForExistingObject),DevExpress.ExpressApp.ConditionalAppearance.AppearanceItemType.ViewItem, "IsNewObject=false") 
            => Enabled = false;
    }
}