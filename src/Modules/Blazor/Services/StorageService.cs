using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Xpand.XAF.Modules.Blazor.Services {
    public static class StorageService {
	    public static IObservable<Unit> SaveCookie(this XafApplication application, string key, string value,int? days =null) 
		    => new Cookie(application).SetValue(key,value,days);

        // public static void SaveCookie(this XafApplication application, string key, string value,int? days =null) {
        //     var httpContextResponse = application.ToBlazor().ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext.Response;
        //     if (days != null) {
        //         httpContextResponse.Cookies.Append(key, value, new CookieOptions() { Expires = DateTimeOffset.Now.AddDays(days.Value) });
        //     }
        //     else
        //         httpContextResponse.Cookies.Append(key, value);
        //
        // }

        public static string ReadCookie(this XafApplication application, string key) 
	        => application.ToBlazor().ServiceProvider.GetRequiredService<IHttpContextAccessor >()
		        .HttpContext.Request.Cookies[key];

        public static IObservable<string> GetClientItem(this XafApplication application, string name)
            => Observable.FromAsync(async () => {
                var invokeAsync = await application.GetRequiredService<IJSRuntime>()
                    .InvokeAsync<object>("localStorage.getItem", name);
                return $"{invokeAsync}";
            });


        public static IObservable<Unit> SaveClientItem(this XafApplication application,string name,object value) 
            => Observable.FromAsync(async () => {
                await application.GetRequiredService<IJSRuntime>()
                    .InvokeAsync<object>("localStorage.setItem", name, value);
                return Unit.Default;
            });
        
        public static IObservable<T> RemoveClientItem<T>(this XafApplication application,string name) 
            => application.GetRequiredService<IJSRuntime>()
                .InvokeAsync<T>("localStorage.removeItem", name)
                .AsTask().ToObservable();

    }
    class Cookie {
        readonly IJSRuntime _jsRuntime;
        string _expires = "";

        public Cookie(XafApplication application) {
            _jsRuntime = application.ToBlazor().ServiceProvider.GetRequiredService<IJSRuntime>();
            ExpireDays = 300;
        }

        public IObservable<Unit> SetValue(string key, string value, int? days = null) {
            var curExp = (days != null) ? (days > 0 ? DateToUtc(days.Value) : "") : _expires;
            return Observable.FromAsync(() => SetCookie($"{key}={value}; expires={curExp}; path=/"));
        }

        public async Task<string> GetValue(string key, string def = "") {
            var cValue = await GetCookie();
            if (string.IsNullOrEmpty(cValue)) return def;
            var strings = cValue.Split(';');
            foreach (var val in strings)
                if (!string.IsNullOrEmpty(val) && val.IndexOf('=') > 0)
                    if (val.Substring(1, val.IndexOf('=') - 1).Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                        return val.Substring(val.IndexOf('=') + 1);
            return def;
        }

        private Task SetCookie(string value) => _jsRuntime.InvokeVoidAsync("eval", $"document.cookie = \"{value}\"").AsTask();

        private async Task<string> GetCookie() {
            return await _jsRuntime.InvokeAsync<string>("eval", "document.cookie");
        }

        public int ExpireDays {
            set => _expires = DateToUtc(value);
        }

        private static string DateToUtc(int days) => DateTime.Now.AddDays(days).ToUniversalTime().ToString("R");
    }
}