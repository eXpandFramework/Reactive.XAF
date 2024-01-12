using System;
using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.Reactive.Services;
public interface IObjectSelector<T> where T : class{
    IObservable<T> SelectObject(ListView source, params T[] objects);
}