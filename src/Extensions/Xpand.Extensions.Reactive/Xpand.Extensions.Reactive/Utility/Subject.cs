﻿using System.Reactive.Subjects;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static void OnNext<T>(this ISubject<T> subject) => subject.OnNext(default);
        public static T PushNext<T>(this ISubject<T> subject,T value){
            subject.OnNext(value);
            return value;
        }
    }
}