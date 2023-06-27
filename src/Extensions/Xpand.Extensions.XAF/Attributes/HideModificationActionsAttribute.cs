namespace Xpand.Extensions.XAF.Attributes{
    public class HideModificationActionsAttribute:HiddenActionAttribute {
        public HideModificationActionsAttribute() : base("Save","New","SaveAndNew","Delete","SaveAndClose"){
        }
    }
}