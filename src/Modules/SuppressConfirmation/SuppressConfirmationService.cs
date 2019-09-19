using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.SuppressConfirmation{
    public static class SuppressConfirmationService{
        internal static IObservable<TSource> TraceSuppressConfirmationModule<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0){
            return source.Trace(name, SuppressConfirmationModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }

        public const ModificationsHandlingMode ModificationsHandlingMode = (ModificationsHandlingMode) (-1);

        internal static IObservable<Unit> Connect(this XafApplication application){
            if (application != null){
                var viewSignal = application
                    .WhenObjectViewCreated()
                    .Where(view => ((IModelObjectViewSupressConfirmation) view.Model).SupressConfirmation)
                    .SelectMany(view => {
                        var whenNewDetailViewObjectChangedOnce = Observable.Empty<Unit>();
                        if (view is DetailView detailView && detailView.ObjectSpace.IsNewObject(detailView.CurrentObject)){
                            whenNewDetailViewObjectChangedOnce = detailView.ObjectSpace.WhenObjectChanged()
                                .Select(tuple => tuple).FirstAsync().ToUnit();
                        }
                        return whenNewDetailViewObjectChangedOnce
                            .Merge(view.ObjectSpace.WhenCommited().Select(tuple => tuple).ToUnit()).ToUnit();
                    }).Publish().RefCount();


                var suppressConfirmationWindows = application.WhenSuppressConfirmationWindows().Publish().RefCount();
                return viewSignal.CombineLatest(suppressConfirmationWindows, (unit, frame) => ChangeModificationHandlingMode(frame)).Merge().ToUnit()
                    .Merge(suppressConfirmationWindows.Select(DisableWebControllers).ToUnit())
                    .ToUnit();
            }
            return Observable.Empty<Unit>();
        }

        private static IObservable<Frame> DisableWebControllers(Frame window){
            var strings = new[]{"ASPxGridListEditorConfirmUnsavedChangesController","WebConfirmUnsavedChangesDetailViewController"};
            return window.Controllers.Cast<Controller>()
                .Where(controller => strings.Any(name => controller.GetType().Name == name))
                .Select(controller => {
                    controller.Active[SuppressConfirmationModule.CategoryName] = false;
                    return window;
                }).ToArray().ToObservable();
        }

        private static IObservable<Unit> ChangeModificationHandlingMode(Frame window){
            window.GetController<ModificationsController>().ModificationsHandlingMode = ModificationsHandlingMode;
            return Observable.Empty<Unit>();
        }

        public static IObservable<Frame> WhenSuppressConfirmationWindows(this XafApplication application){
            return application.WhenWindowCreated().Cast<Frame>()
                .Merge(application.WhenNestedFrameCreated().Cast<Frame>())
                .WhenModule(typeof(SuppressConfirmationModule))
                .ViewChanged().Where(_ => _.frame.View is ObjectView)
                .Where(_ => ((IModelObjectViewSupressConfirmation) _.frame.View.Model).SupressConfirmation)
                .Select(_ => _.frame).Publish().RefCount();
        }
    }
}