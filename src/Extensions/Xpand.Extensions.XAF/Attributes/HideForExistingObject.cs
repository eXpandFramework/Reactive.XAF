using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;

namespace Xpand.Extensions.XAF.Attributes{
    public class HideForExistingObject:AppearanceAttribute {
        public HideForExistingObject():base(nameof(DisableForExistingObject),DevExpress.ExpressApp.ConditionalAppearance.AppearanceItemType.ViewItem, "IsNewObject=false") 
            => Visibility=ViewItemVisibility.Hide;
    }
}