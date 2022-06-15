using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Microsoft.Graph;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
    public static class RequestCustomization{
        static readonly Subject<IBaseRequest> CustomizeSubject = new Subject<IBaseRequest>();

        
        public static IObservable<IBaseRequest> Customize => CustomizeSubject.AsObservable();

        
        public static Func<IBaseRequest, IBaseRequest> Default { get; } = request => {
            CustomizeSubject.OnNext(request);
            return request;
        };
    }
}