using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Google.Apis.Requests;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.Office.Cloud.Google{
    public static class RequestCustomization{
        static readonly Subject<ClientServiceRequest> CustomizeSubject = new Subject<ClientServiceRequest>();

        [PublicAPI]
        public static IObservable<ClientServiceRequest> Customize => CustomizeSubject.AsObservable();

        [PublicAPI]
        public static Func<ClientServiceRequest, ClientServiceRequest> Default{ get; } = request => {
            CustomizeSubject.OnNext(request);
            return request;
        };
    }
}