using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Utils;
using DevExpress.XAF.Modules.Reactive.Extensions;
using DevExpress.XAF.Modules.Reactive.Services;

namespace DevExpress.XAF.Modules.Reactive.Services{
    public static class ActionExtensions{

        public static IObservable<(TAction action, BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged<TAction>(
            this TAction source,Func<TAction,BoolList> boolListSelector ) where TAction:ActionBase{
            return Observable.Return(boolListSelector(source)).ResultValueChanged()
                .Select(tuple => (source,tuple.boolList, tuple.e))
                .TakeUntil(WhenDisposing(source));
        }

        public static IObservable<(SimpleAction simpleAction, SimpleActionExecuteEventArgs e, IObjectSpace objectSpace,
            Controller controller, View view, XafApplication application)> WhenExecuted(this SimpleAction simpleAction){
            return Observable.Return(simpleAction).Executed();
        }

        public static IObservable<(SingleChoiceAction simpleAction, SingleChoiceActionExecuteEventArgs e, IObjectSpace objectSpace
            , Controller controller, View view, XafApplication application)> WhenExecuted(
            this SingleChoiceAction singleChoiceAction){

            return Observable.Return(singleChoiceAction).Executed();
        }

        public static IObservable<(SimpleAction simpleAction, SimpleActionExecuteEventArgs e,IObjectSpace objectSpace,Controller controller,View view,XafApplication application)> Executed(
            this IObservable<SimpleAction> source){

            return source.Executed<SimpleAction, SimpleActionExecuteEventArgs>();
        }


        public static IObservable<(TAction, EventArgs args)> WhenDisposing<TAction>(this TAction simpleAction) where TAction:ActionBase{
            return Disposing<TAction>(Observable.Return(simpleAction));
        }

        public static IObservable<TAction> WhenActionActivated<TAction>(this TAction simpleAction) where TAction:ActionBase{
            return simpleAction.ResultValueChanged(action => action.Active).Where(tuple => tuple.action.Active.ResultValue)
                .Select(_ => _.action);
        }

        public static IObservable<(TAction, EventArgs args)> Disposing<TAction>(
            this IObservable<ActionBase> source) where TAction:ActionBase{

                return source
                    .SelectMany(item => {
                        return Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Disposing += h, h => item.Disposing -= h);
                    })
                    .Select(pattern => pattern)
                    .TransformPattern<EventArgs,TAction>()
                ;

        }

        public static IObservable<(SingleChoiceAction simpleAction, SingleChoiceActionExecuteEventArgs e,IObjectSpace objectSpace,Controller controller,View view,XafApplication application)> Executed(
            this IObservable<SingleChoiceAction> source){

            return source.Executed<SingleChoiceAction, SingleChoiceActionExecuteEventArgs>();
        }

        public static IObservable<(TAction simpleAction, TActionBaseEventArgs e,IObjectSpace objectSpace,Controller controller,View view,XafApplication application)> Executed<TAction,TActionBaseEventArgs>(
            this IObservable<TAction> source)where TAction:ActionBase where TActionBaseEventArgs:ActionBaseEventArgs{
            
            var observable = source
                .ExecutedPattern()
                .Select(tuple => {
                    var e = (TActionBaseEventArgs) tuple.EventArgs;
                    var controller = e.Action.Controller;
                    var view = controller.Frame.View;
                    return ((TAction) tuple.Sender, e,view.ObjectSpace,controller,view,controller.Application);
                });
            return observable.TakeUntil(Disposing<TAction>(source));
        }

        private static IObservable<EventPattern<ActionBaseEventArgs>> ExecutedPattern<TAction>(
            this IObservable<TAction> source) where TAction : ActionBase{
            return source
                .SelectMany(item => {
                    return Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
                        h => item.Executed += h, h => item.Executed -= h);
                });


        }
    }
}