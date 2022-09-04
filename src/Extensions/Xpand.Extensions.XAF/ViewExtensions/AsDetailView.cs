using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtensions{
    public static partial class ViewExtensions{
	    public static DetailView AsDetailView(this View view) => view as DetailView;
	    public static DetailView ToDetailView(this View view) => (DetailView)view;
    }
}