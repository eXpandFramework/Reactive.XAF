using System.Windows.Forms;
using DevExpress.ExpressApp.Templates;

namespace Xpand.XAF.Modules.Reactive.Win.Services{
    public static class TemplateExtension{
        public static Form ToForm(this IFrameTemplate frameTemplate){
            return (Form) frameTemplate;
        }
    }
}