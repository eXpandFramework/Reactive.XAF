using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.XAF.Modules.Reactive.Rest.Extensions {
    public static class RestAttributeExtensions {
        private static HttpMethod HttpMethod(this IRestAttribute attribute) 
            => attribute.HttpMethod == null && attribute is RestOperationAttribute operationAttribute
                ? operationAttribute.Operation == Operation.Get ? System.Net.Http.HttpMethod.Get
                    : operationAttribute.Operation == Operation.Create || operationAttribute.Operation == Operation.Delete
                        ? System.Net.Http.HttpMethod.Post : operationAttribute.Operation == Operation.Update
                            ? new HttpMethod("PATCH") : new HttpMethod(attribute.HttpMethod!)
                : new HttpMethod(attribute.HttpMethod!);

        internal static string RequestUrl(this IRestAttribute operationAttribute,object instance) {
            var regexObj = new Regex("(.*){([^}]*)}(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var match = regexObj.Match(operationAttribute.RequestUrl);
            if (match.Success) {
                var value = match.Groups[2].Value;
                value = $"{instance.GetTypeInfo().FindMember(value).GetValue(instance)}";
                return Regex.Replace(operationAttribute.RequestUrl, "(?<before>.*){([^}]*)}(?<after>.*)", $"${{before}}{value}${{after}}",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline).TrimEnd('/');
            }

            return operationAttribute.RequestUrl.TrimEnd('/');
        }

        internal static IObservable<object> Send(this IRestAttribute attribute,  object instance,ICredentialBearer user,string requestUrl=null,Func<HttpResponseMessage, IObservable<object>> deserializeResponse=null) {
            requestUrl ??= attribute.RequestUrl(instance);
            var url = $"{user.BaseAddress}{requestUrl}";
            var pollInterval = attribute.PollInterval>0?TimeSpan.FromSeconds(attribute.PollInterval) :(TimeSpan?) null ;
            return attribute.HttpMethod().Send(url, instance, user.Key, user.Secret,deserializeResponse,pollInterval)
                .Select(o => o)
                .HandleAttributeErrors(url,instance,attribute.HandleErrors);
        }

        static IObservable<object> HandleAttributeErrors(this IObservable<object> source, string url, object o, bool handleErrors) 
            => source.Catch<object, Exception>(exception => {
                var message = $"{url}-{o}";
                return handleErrors
                    ? message.Observe().IgnoreElements()
                    : Observable.Throw<object>(new Exception(new[]{exception.Message,message}.JoinNewLine(), exception));
            });
    }
}