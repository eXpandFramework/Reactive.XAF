using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.SuppressConfirmation{
    public static class SuppressConfirmationService{
        public const ModificationsHandlingMode ModificationsHandlingMode = (ModificationsHandlingMode) (-1);

        internal static IObservable<Unit> Connect(){
            var viewSignal = RxApp.Application.WhenModule(typeof(SuppressConfirmationModule))
                .ObjectViewCreated()
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
            

            return viewSignal.CombineLatest(Windows, (unit, frame) => ChangeModificationHandlingMode(frame)).Merge().ToUnit()
                .Merge(Windows.Select(DisableWebControllers).ToUnit())
                .TakeUntilDisposingMainWindow()
                .ToUnit();
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

        public static IObservable<Frame> Windows{ get; } = RxApp.Windows.Cast<Frame>()

            .Merge(RxApp.NestedFrames.Cast<Frame>())
            .WhenModule(typeof(SuppressConfirmationModule))
            .ViewChanged().Where(_ => _.frame.View is ObjectView)
            .Where(_ => ((IModelObjectViewSupressConfirmation) _.frame.View.Model).SupressConfirmation)
            .Select(_ => _.frame).Publish().RefCount();

    }
}