using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.DetailViewExtensions {
    public static partial class DetailViewExtensions {
        public static T SetCurrentObject<T>(this DetailView detailView, T currentObject) where T : class 
            => (T)(detailView.CurrentObject = detailView.ObjectSpace.GetObject(currentObject));

        public static T SetCurrentObject<T>(this DetailView detailView, Func<IObjectSpace, T> selector) where T:class
            => (T)(detailView.CurrentObject = selector(detailView.ObjectSpace));
    }
}