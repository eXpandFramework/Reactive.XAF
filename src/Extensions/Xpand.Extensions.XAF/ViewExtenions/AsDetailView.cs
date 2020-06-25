using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.ViewExtenions{
    public static partial class ViewExtenions{
	    public static DetailView AsDetailView(this View view) => view as DetailView;
    }
}