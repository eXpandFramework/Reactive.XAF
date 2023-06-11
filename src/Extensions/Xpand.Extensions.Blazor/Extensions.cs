using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.AmbientContext;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Observable = System.Reactive.Linq.Observable;

namespace Xpand.Extensions.Blazor {
    public static class Extensions {
        public static BlazorApplication ToBlazor(this XafApplication application) => (BlazorApplication) application;

        public static async Task RunWithStorageAsync(this IServiceProvider provider, Action<BlazorApplication> selector,string marker=null)
            => await provider.RunWithStorageAsync(() => Observable.Return(Unit.Default).Do(_ => selector(provider.GetApplication(marker))));

        public static BlazorApplication GetApplication(this IServiceProvider provider,string marker=null) {
            ValueManager.GetValueManager<bool>(marker??XafApplicationExtensions.ApplicationMarker).Value = true;
	        return provider.GetRequiredService<IXafApplicationProvider>().GetApplication();
        }

        public static Task<T> RunWithStorageAsync<T>(this IServiceProvider provider,Func<BlazorApplication,IObservable<T>> selector,string marker=null)
            => provider.RunWithStorageAsync(application => selector(application).ToTask(),marker);
        
        public static async Task<T> RunWithStorageAsync<T>(this IServiceProvider provider,Func<BlazorApplication,Task<T>> selector,string marker=null)
            => await provider.RunWithStorageAsync(async () => await selector(provider.GetApplication(marker)));

        public static void RunIsolated(this IServiceProvider provider, Action<BlazorApplication> action, string marker = null)
            => ValueManagerContext.RunIsolated(() => action(provider.GetApplication(marker)));
        
        public static void RunWithStorage(this IServiceProvider provider, Action<BlazorApplication> action, string marker=null)
	        => provider.RunWithStorage(() => action(provider.GetApplication(marker)));

        public static void RunWithStorage(this IServiceProvider provider, Action action)
	        => provider.GetRequiredService<IValueManagerStorageContext>().RunWithStorage(action);

        public static async Task<T> RunWithStorageAsync<T>(this IServiceProvider provider,Func<IObservable<T>> selector) 
	        => await provider.RunWithStorageAsync(selector().ToTask);
        
        public static async Task<T> RunWithStorageAsync<T>(this IServiceProvider provider,Func<Task<T>> selector) 
	        => await provider.GetRequiredService<IValueManagerStorageContext>()
		        .RunWithStorageAsync(selector);
    }
}
