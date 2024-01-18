using System;
using DevExpress.ExpressApp;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib {
    public class ObjectSelector<T> : IObjectSelector<T> where T : class{
        public IObservable<T> SelectObject(ListView view, params T[] objects) 
            => view.SelectObject(objects);
    }

}