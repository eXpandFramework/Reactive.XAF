using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Utils;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ActionExtensions{
        public static IObservable<TFrame> WhenView<TFrame>(this IObservable<TFrame> source, Type objectType)
            where TFrame : Frame{
            return source.SelectMany(frame => frame.View.AsObservable().When(objectType).Select(view => frame));
        }

        public static IObservable<TAction> When<TAction>(this IObservable<TAction> source,Type objectType)where TAction : ActionBase{
            return source.Where(_ => objectType.IsAssignableFrom(_.Controller.Frame.View.ObjectTypeInfo.Type));
        }

        public static IObservable<IObjectSpace> ToObjectSpace<TAction>(this IObservable<TAction> source) where TAction:ActionBase{
            return source.Select(_ => _.Controller.Frame.View.ObjectSpace);
        }

        public static IObservable<(TAction action, CancelEventArgs e)> WhenExecuting<TAction>(this TAction action) where TAction : ActionBase{
            return Observable.FromEventPattern<CancelEventHandler, CancelEventArgs>(h => action.Executing += h,
                    h => action.Executing -= h)
                .TransformPattern<CancelEventArgs, TAction>();
        }

        public static IObservable<(TAction action,Type objectType,View view,Frame frame,IObjectSpace objectSpace,ShowViewParameters showViewParameters)> ToParameter<TAction>(
            this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction:ActionBase{
            return source.Select(_ => {
                var frame = _.action.Controller.Frame;
                return (_.action, frame.View.ObjectTypeInfo.Type, frame.View, frame, frame.View.ObjectSpace,_.e.ShowViewParameters);
            });
        }
        public static IObservable<TAction> ToAction<TAction>(this IObservable<(TAction action, ActionBaseEventArgs e)> source)
            where TAction : ActionBase{
            return source.Select(_ => _.action);
        }

        public static IObservable<(TAction action, ActionBaseEventArgs e)> WhenExecuted<TAction>(this TAction action) where TAction : ActionBase{
            return Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(h => action.Executed += h,
                    h => action.Executed -= h)
                .TransformPattern<ActionBaseEventArgs, TAction>();
        }

        public static IObservable<(TAction action, ActionBaseEventArgs e)> WhenExecuteCompleted<TAction>(this TAction action) where TAction:ActionBase{

            return Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(h => action.ExecuteCompleted += h,
                    h => action.ExecuteCompleted -= h)
                .TransformPattern<ActionBaseEventArgs, TAction>();
        }

        public static IObservable<(TAction action, BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged<TAction>(
            this TAction source,Func<TAction,BoolList> boolListSelector ) where TAction:ActionBase{
            return Observable.Return(boolListSelector(source))
                    .ResultValueChanged().Select(tuple => (source,tuple.boolList, tuple.e))
                ;
        }

        public static IObservable<Unit> WhenDisposing<TAction>(this TAction simpleAction) where TAction:ActionBase{
            return Disposing(Observable.Return(simpleAction));
        }

        public static IObservable<TAction> WhenActionActivated<TAction>(this TAction simpleAction) where TAction:ActionBase{
            return simpleAction.ResultValueChanged(action => action.Active).Where(tuple => tuple.action.Active.ResultValue)
                .Select(_ => _.action);
        }

        public static IObservable<Unit> Disposing<TAction>(
            this IObservable<TAction> source) where TAction:ActionBase{
            return source
                    .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Disposing += h,h => item.Disposing -= h)
                    .Select(pattern => pattern)
                    .ToUnit());

        }



    }
}