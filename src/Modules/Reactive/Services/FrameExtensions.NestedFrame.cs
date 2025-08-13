using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;

namespace Xpand.XAF.Modules.Reactive.Services;
    public partial class FrameExtensions {
        public static IObservable<TFrame> WhenIsNotOnLookupPopupTemplate<TFrame>(this IObservable<TFrame> source)
            where TFrame : Frame{
            return source.Where(frame => !(frame.Template is ILookupPopupFrameTemplate))
                .Cast<TFrame>();
        }

        public static IObservable<TFrame> WhenIsOnLookupPopupTemplate<TFrame>(this IObservable<TFrame> source)
            where TFrame : Frame{
            return source.Where(frame => frame.Template is ILookupPopupFrameTemplate)
                .Cast<TFrame>();
        }
    }