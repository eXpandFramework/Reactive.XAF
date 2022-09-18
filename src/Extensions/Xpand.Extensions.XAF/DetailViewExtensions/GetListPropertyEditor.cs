using System;
using System.Linq.Expressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;

namespace Xpand.Extensions.XAF.DetailViewExtensions{
    public static partial class DetailViewExtensions{
        public static ListPropertyEditor GetListPropertyEditor<TObject>(this DetailView detailView, Expression<Func<TObject, object>> memberName)
            => detailView.GetPropertyEditor<ListPropertyEditor, TObject>(memberName);
    }
}