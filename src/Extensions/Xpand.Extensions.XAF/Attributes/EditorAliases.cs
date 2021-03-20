using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Xpand.Extensions.XAF.Attributes {
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct EditorAliases {
        public const string UploadFile = "UploadFile";
        public const string Tag = "Tag";
        public const string MarkupContent = "MarkupContent";
        public const string DisplayText = "DisplayText";
        public const string BlazorLookup = "BlazorLookup";
    
    }
}