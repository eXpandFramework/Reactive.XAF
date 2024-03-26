using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Blazor.Services{
    public class ObjectSelector<T> : IObjectSelector<T> where T : class{
        public IObservable<T> SelectObject(ListView view, params T[] objects) {
            var viewEditor = (view.Editor as DxGridListEditor);
            if (viewEditor == null)
                throw new NotImplementedException(nameof(ListView.Editor));
            viewEditor.UnselectObjects(viewEditor.GetSelectedObjects());
            return objects.ToNowObservable()
                .Do(obj => viewEditor.SelectObject(obj));
        }
    }
}