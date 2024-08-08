using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.ViewVariantsModule;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.TestsLib.Common {
    public static class AssertExtensions {
        public static TimeSpan TimeoutInterval = (Debugger.IsAttached ? 120 : 15).Seconds();
        public static IObservable<SimpleAction> AssertSimpleAction(this IObservable<Frame> source, string actionId,Func<SimpleAction,bool> completeWhenNotAvailable=null,[CallerMemberName]string caller="")
            => source.SelectMany(frame => frame.AssertSimpleAction(actionId,completeWhenNotAvailable, caller));

        public static IObservable<SimpleAction> AssertSimpleAction(this Frame frame,string actionId,Func<SimpleAction,bool> completeWhenNotAvailable=null,[CallerMemberName]string caller="") 
            => frame.Actions<SimpleAction>(actionId).ToNowObservable()
                .SelectMany(action => !action.Available() && (completeWhenNotAvailable?.Invoke(action) ?? false)
                    ? Observable.Empty<SimpleAction>()
                    : action.Observe().Assert($"{caller} {frame.View} {actionId}")
                        .Select(simpleAction => simpleAction));
        
        public static IObservable<SingleChoiceAction> AssertSingleChoiceAction(this IObservable<Frame> source,
            string actionId, Func<SingleChoiceAction,int> itemsCount = null) 
            => source.AssertSingleChoiceAction(actionId,(action, item) => item==null? itemsCount?.Invoke(action) ?? -1:-1);
        
        public static IObservable<SingleChoiceAction> AssertSingleChoiceAction(this IObservable<Frame> source,
            string actionId, Func<SingleChoiceAction,ChoiceActionItem, int> itemsCount = null) 
            => source.SelectMany(frame => frame.AssertSingleChoiceAction(actionId, itemsCount))
                .ReplayFirstTake();

        public static IObservable<SingleChoiceAction> AssertSetFilterAction(this Frame frame,  int count,string triggerId)
            => frame.AssertSingleChoiceAction("SetFilter", _ => count).ConcatIgnored(action => action.Trigger(() => action.Items.FindItemByID(triggerId)));
        
        public static IObservable<SingleChoiceAction> AssertSingleChoiceAction(this Frame frame, string actionId, Func<SingleChoiceAction,  int> itemsCount = null)
            => frame.AssertSingleChoiceAction(actionId, (action, item) => item!=null?-1:itemsCount?.Invoke(action) ?? -1);

        public static IObservable<SingleChoiceAction> AssertSingleChoiceAction(this Frame frame,string actionId, Func<SingleChoiceAction, ChoiceActionItem, int> itemsCount=null)
            => frame.Actions<SingleChoiceAction>(actionId)
                .Where(action => action.Available() || itemsCount != null && itemsCount(action, null) == -1).ToNowObservable()
                .Assert($"{nameof(AssertSingleChoiceAction)} {actionId}")
                .SelectMany(action => {
                    var invoke = itemsCount?.Invoke(action, null) ?? -1;
                    return action.AssertSingleChoiceActionItems(action.Items.Active().ToArray(),
                        invoke, item => itemsCount?.Invoke(action, item) ?? -1).IgnoreElements().Concat(action.Observe());
                }).ReplayFirstTake();

        static IObservable<SingleChoiceAction> AssertSingleChoiceActionItems(
            this SingleChoiceAction action, ChoiceActionItem[] source, int itemsCount,Func<ChoiceActionItem,int> nestedItemsCountSelector=null,[CallerMemberName]string caller="") 
            => source.Active().ToArray().Observe().Where(items => items.Length==itemsCount||itemsCount==-1)
                
                .Assert($"{action.Id} has {source.Active().ToArray().Length} items ({source.JoinString(", ")}) but should have {itemsCount}",caller:caller)
                .IgnoreElements().SelectMany()
                .Concat(source.Active().ToArray().ToNowObservable())
                .SelectMany(item => {
                    var count = nestedItemsCountSelector?.Invoke(item) ?? -1;
                    return count > -1 ? action.AssertSingleChoiceActionItems(item.Items.Active().ToArray(), count) : Observable.Empty<SingleChoiceAction>();
                })
                .IgnoreElements();
        
        public static IObservable<TObject[]> AssertProviderObjects<TObject>(this XafApplication application, TimeSpan? timeout = null, [CallerMemberName] string caller = "") where TObject : class
            => application.WhenProviderObjects<TObject>().Assert(timeout: timeout, caller: caller);
        
        public static IObservable<Frame> AssertListViewHasObject<TObject>(this XafApplication application, Func<TObject,bool> matchObject=null,int count=0,TimeSpan? timeout=null,[CallerMemberName]string caller="") where TObject : class
            => application.WhenFrame(typeof(TObject),ViewType.ListView)
                .SelectMany(frame => frame.AssertListViewHasObject(matchObject, count,timeout, caller))
                .ReplayFirstTake();
        public static IObservable<Frame> AssertListViewHasObject<TObject>(this XafApplication application,Func<string> navigationItemId, Func<TObject,bool> matchObject=null,int count=0,TimeSpan? timeout=null,[CallerMemberName]string caller="") where TObject : class 
            => application.AssertNavigation(navigationItemId).Zip(application.AssertListViewHasObject(matchObject,count,timeout,caller)).ToSecond().ReplayFirstTake();

        public static IObservable<Frame> AssertListViewHasObject<TObject>(this Frame frame,Func<TObject, bool> matchObject=null, int count=0,TimeSpan? timeout=null, [CallerMemberName]string caller="") where TObject : class
            => frame.View.WhenControlsCreated(true).Take(1)
                .Select(view => frame is NestedFrame nestedFrame ? nestedFrame.ViewItem.View.WhenCurrentObjectChanged().StartWith(view)
                        .SelectMany(_ => frame.AssertListViewHasObject(matchObject, count, timeout, caller, view))
                    : frame.AssertListViewHasObject(matchObject, count, timeout, caller, view)).Switch();

        private static IObservable<Frame> AssertListViewHasObject<TObject>(this Frame frame, Func<TObject, bool> matchObject, int count, TimeSpan? timeout, string caller, View view) where TObject : class
            => view.ToListView().WhenObjects<TObject>()
                .Where(objects => count == 0 || objects.Length == count)
                .SelectMany(objects => objects.Where(value => matchObject?.Invoke(value)??true).ToNowObservable())
                .TakeUntil(_ => view.IsDisposed)
                .Select(arg => view.ObjectSpace?.GetObject(arg))
                .WhenNotDefault()
                .SelectMany(value => frame.Application.GetRequiredService<IObjectSelector<TObject>>().SelectObject(view.ToListView(),value))
                // .SelectMany(value => view.ToListView().SelectObject(value)).Select(o => o)
                .Assert(_ => $"{caller}, {typeof(TObject).Name}-{view.Id}",timeout:timeout)
                .ReplayFirstTake().To(frame)
                .Select(frame1 => frame1);

        public static IObservable<TTabbedControl> AssertTabControl<TTabbedControl>(this XafApplication application,Type objectType=null,Func<DetailView,bool> match=null,Func<IModelTabbedGroup, bool> tabMatch=null,TimeSpan? timeout=null,[CallerMemberName]string caller="") 
            => application.WhenTabControl<TTabbedControl>( objectType, match,tabMatch).Assert(objectType?.Name,caller:$"{caller} - {nameof(AssertTabControl)}",timeout:timeout);
        
        public static IObservable<TSource> Assert<TSource>(this IObservable<TSource> source, TimeSpan? timeout = null, [CallerMemberName] string caller = "") 
            => source.Assert(_ => "",timeout,caller);

        public static IObservable<TSource> Assert<TSource>(this IObservable<TSource> source, string message, TimeSpan? timeout = null,[CallerMemberName]string caller="")
            => source.Assert(_ => message,timeout,caller);

        
        public static TimeSpan? DelayOnContextInterval=250.Milliseconds();
        public static IObservable<TSource> Assert<TSource>(this IObservable<TSource> source,Func<TSource,string> messageFactory,TimeSpan? timeout=null,[CallerMemberName]string caller=""){
            var timeoutMessage = messageFactory.MessageFactory(caller);
            return source.ReplayFirstTake().Log(messageFactory,Console.Out, caller).ThrowIfEmpty(timeoutMessage).Timeout(timeout ?? TimeoutInterval, timeoutMessage)
                .DelayOnContext(DelayOnContextInterval)
                ;
        }
        
        public static string MessageFactory<TSource>(this Func<TSource, string> messageFactory, string caller) => $"{caller}: {messageFactory(default)}";
        
        public static IObservable<Unit> AssertNavigation(this XafApplication application,Func<string> view, Func<IObservable<Frame>,IObservable<Unit>> assert, IObservable<Unit> canNavigate) 
            => application.AssertNavigation(view,_ => canNavigate.SwitchIfEmpty(Observable.Throw<Unit>(new CannotNavigateException())).ToUnit())
                .SelectMany(window => window.Observe().SelectMany(frame => assert(frame.Observe())))
                .FirstOrDefaultAsync().ReplayFirstTake();
        
        public static IObservable<Frame> AssertChangeViewVariant(this IObservable<Frame> source,string id) 
            => source.ToController<ChangeVariantController>()
                .SelectMany(controller => {
                    var choiceActionItem = controller.ChangeVariantAction.Items.First(item => item.Id == id);
                    var variantInfo = ((VariantInfo)choiceActionItem.Data);
                    return (variantInfo.ViewID != controller.Frame.View.Id
                        ? controller.ChangeVariantAction.Trigger(controller.Application.WhenFrame(variantInfo.ViewID)
                                .Merge(controller.Frame.WhenFrame(variantInfo.ViewID)).Take(1),
                            () => choiceActionItem)
                        : controller.Frame.Observe())
                        .Assert();
                }).ReplayFirstTake();
        
        public static IObservable<Window> AssertNavigation(this XafApplication application, Func<string> viewId,Func<Window,IObservable<Unit>> navigate=null)
            => application.WhenMainWindowCreated()
                .SelectMany(_ => application.Navigate(viewId(),window => (navigate?.Invoke(window)?? Observable.Empty<Unit>()).SwitchIfEmpty(Unit.Default.Observe()))
                    .Assert($"{viewId()}")
                    .Catch<Window,CannotNavigateException>(_ => Observable.Empty<Window>())).ReplayFirstTake()
            ;
        
        public static IObservable<Frame> AssertLinkObject<TObject>(this Frame frame) where TObject : class
            => frame.NestedListViews(typeof(TObject)).Take(1)
                .Select(propertyEditor => propertyEditor.Frame.GetController<LinkUnlinkController>().LinkAction)
                .SelectMany(linkAction => frame.Application.WhenProviderObject<TObject>().Assert().ObserveOnContext()
                    .SelectMany(_ => linkAction.LinkObject( ).Assert())).ReplayFirstTake();
        
        public static IObservable<TException> AssertException<TException>(this XafApplication application,
            Func<IObservable<Unit>> other, Func<TException, bool> match = null) where TException : Exception
            => application.Observe().AssertException(other, match);
        
        public static IObservable<TException> AssertException<TException,T>(this IObservable<T> source,Func<IObservable<Unit>> other,Func<TException,bool> match=null) where TException : Exception 
            => TestTracing.Handle(match).MergeToUnit(Unit.Default.Observe().SelectMany(_
                    => other().ThrowOnNext().IgnoreElements().CompleteOnError())).To<TException>()
                .Merge(source.CompleteOnError().IgnoreElements().Select(_ => default(TException))).Skip(1)
                .Assert();
        
        public class CannotNavigateException:Exception;

    }
}