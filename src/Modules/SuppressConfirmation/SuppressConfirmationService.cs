using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.SuppressConfirmation{
    public static class SuppressConfirmationService{
        internal static IObservable<TSource> TraceSuppressConfirmationModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, SuppressConfirmationModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        public const ModificationsHandlingMode ModificationsHandlingMode = (ModificationsHandlingMode) (-1);

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) =>
            manager.WhenApplication(application => {
                var suppressConfirmationWindows = application.WhenSuppressConfirmationWindows().Publish().RefCount();
                return suppressConfirmationWindows
                    .SelectMany(frame => frame.ChangeModificationHandlingMode().To(frame))
                    .SelectMany(frame => {
                        var whenNewDetailViewObjectChangedOnce = Observable.Empty<Unit>();
                        if (frame.View is DetailView detailView && detailView.ObjectSpace.IsNewObject(detailView.CurrentObject)){
                            whenNewDetailViewObjectChangedOnce = detailView.ObjectSpace.WhenObjectChanged()
                                .Select(tuple => tuple).TakeFirst().ToUnit();
                        }
                        return whenNewDetailViewObjectChangedOnce
                            .Merge(frame.View.ObjectSpace.WhenCommitted().Select(tuple => tuple).ToUnit()).ToUnit()
                            .SelectMany(_ => frame.ChangeModificationHandlingMode());
                    }).ToUnit()
                    .Merge(suppressConfirmationWindows.Select(DisableWebControllers).ToUnit());
            });

        private static IObservable<Controller> DisableWebControllers(Frame window) =>
            window.Controllers.Cast<Controller>()
                .Where(controller => new[]{"ASPxGridListEditorConfirmUnsavedChangesController","WebConfirmUnsavedChangesDetailViewController"}
                    .Any(name => controller.GetType().Name == name))
                .Select(controller => {
                    controller.Active[SuppressConfirmationModule.CategoryName] = false;
                    return controller;
                })
                .ToArray().ToObservable()
                .TraceSuppressConfirmationModule(controller => controller.Name);

        private static IObservable<Unit> ChangeModificationHandlingMode(this Frame window){
            var modificationsController = window.GetController<ModificationsController>();
            if (modificationsController != null)
                modificationsController.ModificationsHandlingMode = ModificationsHandlingMode;
            return Observable.Empty<Unit>();
        }

        public static IObservable<Frame> WhenSuppressConfirmationWindows(this XafApplication application) =>
            application.WhenWindowCreated().Cast<Frame>()
                .Merge(application.WhenNestedFrameCreated().Cast<Frame>())
                .WhenModule(typeof(SuppressConfirmationModule))
                .ViewChanged().Where(frame => frame.View is ObjectView)
                .Where(frame => ((IModelObjectViewSupressConfirmation) frame.View.Model).SupressConfirmation)
                .Select(frame => frame)
                .TraceSuppressConfirmationModule(frame => frame.View.Id);
    }
}