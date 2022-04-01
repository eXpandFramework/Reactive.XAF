using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Blazor.Services {
    public static class NavigationService {
        public static IObservable<Unit> NavigateTo(this XafApplication application,string uri,bool endResponse=true) 
            => application.WhenWeb().Do(api => api.Redirect(uri, endResponse)).ToUnit();

        public static IObservable<LocationChangedEventArgs> WhenLocationChanged(this NavigationManager navManager)
            => navManager.WhenEvent<LocationChangedEventArgs>(nameof(NavigationManager.LocationChanged));

        public static string QueryStringItemValue(this NavigationManager navManager, string key) 
            => QueryHelpers.ParseQuery(navManager.ToAbsoluteUri(navManager.Uri).Query)
                .TryGetValue(key, out var stringValues) ? stringValues.FirstOrDefault() : null;

        public static IObservable<string> WhenQueryStringItemValue(this NavigationManager navManager, string key) 
            => QueryHelpers.ParseQuery(navManager.ToAbsoluteUri(navManager.Uri).Query)
                .TryGetValue(key, out var stringValues) ? stringValues.ToNowObservable() : Observable.Empty<string>();
    }
}