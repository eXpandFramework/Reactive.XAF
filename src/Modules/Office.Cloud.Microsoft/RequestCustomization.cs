using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using JetBrains.Annotations;
using Microsoft.Graph;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
    public static class RequestCustomization{
        static readonly Subject<IBaseRequest> CustomizeSubject = new Subject<IBaseRequest>();

        [PublicAPI]
        public static IObservable<IBaseRequest> Customize => CustomizeSubject.AsObservable();

        [PublicAPI]
        public static Func<IBaseRequest, IBaseRequest> Default { get; } = request => {
            CustomizeSubject.OnNext(request);
            return request;
        };
    }
}