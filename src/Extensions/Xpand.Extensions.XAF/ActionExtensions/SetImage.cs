using System.ComponentModel;
using DevExpress.ExpressApp.Actions;
using EnumsNET;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {
        public static void SetImage(this ActionBase action,CommonImage imageName) 
            => action.ImageName=imageName.AsString(EnumFormat.Description);
    }

    public enum CommonImage {
        [Description("Action_Refresh")]
        Refresh,
        [Description("New")]
        New
    }
}