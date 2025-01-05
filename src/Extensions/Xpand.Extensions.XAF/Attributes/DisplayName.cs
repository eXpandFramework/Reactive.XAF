using System.ComponentModel;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.Extensions.XAF.Attributes{
    public class DisplayName(string displayName,bool compound=true) : DisplayNameAttribute(compound?displayName.CompoundName():displayName);
}