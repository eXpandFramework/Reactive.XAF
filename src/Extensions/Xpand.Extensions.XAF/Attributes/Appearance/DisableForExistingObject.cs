namespace Xpand.Extensions.XAF.Attributes.Appearance{
    public class DisableForExistingObject() : DisableWhenAttribute("IsNewObject=false");
}