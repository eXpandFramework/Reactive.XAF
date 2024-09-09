namespace Xpand.Extensions.XAF.Attributes.Appearance{
    public class HideModificationActionsAttribute:HiddenActionAttribute {
        public HideModificationActionsAttribute() : base("Save","New","SaveAndNew","Delete","SaveAndClose"){
        }
    }
}