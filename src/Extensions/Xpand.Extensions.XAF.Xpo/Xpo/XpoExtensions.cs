using DevExpress.Xpo;

namespace Xpand.Extensions.XAF.Xpo.Xpo {
    public static class XpoExtensions {
        public static void FireChanged(this IXPReceiveOnChangedFromArbitrarySource source, string propertyName) => source.FireChanged(propertyName);
    }
}