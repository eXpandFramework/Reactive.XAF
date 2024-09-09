using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;

namespace Xpand.Extensions.XAF.Attributes.Appearance{
    public class HideWhenAttribute:AppearanceAttribute {
        public HideWhenAttribute(string criteria):base($"{nameof(HideWhenAttribute)}_{criteria}",DevExpress.ExpressApp.ConditionalAppearance.AppearanceItemType.ViewItem, criteria) 
            => Visibility=ViewItemVisibility.Hide;
    }
    public class HideForExistingObject() : HideWhenAttribute("IsNewObject=false");
    public class HideWhenTrueAttribute(string propertyName) : HideWhenAttribute($"{propertyName}=true");
    public class HideWhenFalseAttribute(string propertyName) : HideWhenAttribute($"{propertyName}=false");
    
}