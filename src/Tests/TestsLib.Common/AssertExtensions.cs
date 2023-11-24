using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.ViewVariantsModule;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.TestsLib.Common {
    public static class AssertExtensions {
        public static TimeSpan TimeoutInterval = (Debugger.IsAttached ? 120 : 15).Seconds();
        public static IObservable<SimpleAction> AssertSimpleAction(this IObservable<Frame> source, string actionId,Func<SimpleAction,bool> completeWhenNotAvailable=null,[CallerMemberName]string caller="")
            => source.SelectMany(frame => frame.AssertSimpleAction(actionId,completeWhenNotAvailable, caller));

        public static IObservable<SimpleAction> AssertSimpleAction(this Frame frame,string actionId,Func<SimpleAction,bool> completeWhenNotAvailable=null,[CallerMemberName]string caller="") 
            => frame.Actions<SimpleAction>(actionId).ToNowObservable()
                .SelectMany(action => !action.Available() && (completeWhenNotAvailable?.Invoke(action)??false) ? Observable.Empty<SimpleAction>()
                    : action.Observe().Assert($"{nameof(AssertSimpleAction)} {frame.View} {actionId}", caller: caller));
        
        public static IObservable<Frame> AssertListViewHasObject<TObject>(this XafApplication application, Func<TObject,bool> matchObject=null,int count=0,[CallerMemberName]string caller="")  
            => application.WhenFrame(typeof(TObject),ViewType.ListView)
                .SelectUntilViewClosed(frame => frame.View.WhenControlsCreated(true).Take(1)
                    .SelectMany(view => view.ToListView().WhenObjects<TObject>()
                        .Where(value => matchObject?.Invoke(value)??true)
                        .SkipOrOriginal(count-1).Take(1)
                        .SelectMany(value => view.ToListView().SelectObject(value)).To(frame)
                        .Assert(_ => $"{typeof(TObject).Name}-{view.Id}",caller:caller)))
                .ReplayFirstTake();

        public static IObservable<TTabbedControl> AssertTabControl<TTabbedControl>(this XafApplication application,Type objectType=null,Func<DetailView,bool> match=null,Func<IModelTabbedGroup, bool> tabMatch=null,[CallerMemberName]string caller="") 
            => application.WhenTabControl<TTabbedControl>( objectType, match,tabMatch).Assert(objectType?.Name,caller:$"{caller} - {nameof(AssertTabControl)}");
        public static IObservable<TSource> Assert<TSource>(
            this IObservable<TSource> source, TimeSpan? timeout = null, [CallerMemberName] string caller = "") 
            => source.Assert(_ => "",timeout,caller);

        public static IObservable<TSource> Assert<TSource>(this IObservable<TSource> source, string message, TimeSpan? timeout = null,[CallerMemberName]string caller="")
            => source.Assert(_ => message,timeout,caller);

        
        public static TimeSpan? DelayOnContextInterval=250.Milliseconds();
        public static IObservable<TSource> Assert<TSource>(this IObservable<TSource> source,Func<TSource,string> messageFactory,TimeSpan? timeout=null,[CallerMemberName]string caller=""){
            var timeoutMessage = messageFactory.MessageFactory(caller);
            return source.Log(messageFactory, caller).ThrowIfEmpty(timeoutMessage).Timeout(timeout ?? TimeoutInterval, timeoutMessage)
                .DelayOnContext(DelayOnContextInterval)
                .ReplayFirstTake();
        }

        
        public static string MessageFactory<TSource>(this Func<TSource, string> messageFactory, string caller) => $"{caller}: {messageFactory(default)}";
        
        public static IObservable<Unit> AssertNavigation(this XafApplication application,string view, Func<IObservable<Frame>,IObservable<Unit>> assert, IObservable<Unit> canNavigate) 
            => application.AssertNavigation(view,_ => canNavigate.SwitchIfEmpty(Observable.Throw<Unit>(new CannotNavigateException())).ToUnit())
                .SelectMany(window => window.Observe().SelectMany(frame => assert(frame.Observe())))
                .FirstOrDefaultAsync().ReplayFirstTake();
        
        public static IObservable<Frame> AssertChangeViewVariant(this IObservable<Frame> source,string id) 
            => source.ToController<ChangeVariantController>()
                .SelectMany(controller => {
                    var choiceActionItem = controller.ChangeVariantAction.Items.First(item => item.Id == id);
                    var variantInfo = ((VariantInfo)choiceActionItem.Data);
                    return variantInfo.ViewID != controller.Frame.View.Id ? controller.ChangeVariantAction.Trigger(controller.Application.WhenFrame(variantInfo.ViewID),() => choiceActionItem) : controller.Frame.Observe();
                });
        
        public static IObservable<Window> AssertNavigation(this XafApplication application, string viewId,Func<Window,IObservable<Unit>> navigate=null)
            => application.Navigate(viewId,window => (navigate?.Invoke(window)?? Observable.Empty<Unit>()).SwitchIfEmpty(Unit.Default.Observe()))
                .Assert($"{viewId}").Catch<Window,CannotNavigateException>(_ => Observable.Empty<Window>());
        public class CannotNavigateException:Exception{ }

    }
}