using System;
using DevExpress.ExpressApp;

namespace Xpand.TestsLib.Common;
public interface IObjectSelector<T> where T : class{
    IObservable<T> SelectObject(ListView source, params T[] objects);
}