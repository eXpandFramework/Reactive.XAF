using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DevExpress.ExpressApp.Actions;
using EnumsNET;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {
        
        public static void SetImage(this ActionBase action,CommonImage imageName) {
            action.BeginUpdate();
            action.ImageName = imageName.AsString(EnumFormat.Description);
            action.EndUpdate();
        }

        public static CommonImage CommonImage(this ActionBase action) 
            => Enums.GetValues<CommonImage>().FirstOrDefault(image => image.AsString(EnumFormat.Description)==action.ImageName);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum CommonImage {
        [Description("Action_Refresh")]
        Refresh,
        [Description("New")]
        New,
        [Description("Action_Debug_Start")]
        Start,
        [Description("Stop")]
        Stop,
        [Description("ConvertTo")]
        ConvertTo,
        [Description("BO_Folder")]
        Folder,
        [Description("Language")]
        Language,
        [Description("Find")]
        Find,
        [Description("Actions_Bookmark")]
        Mark
    }
}