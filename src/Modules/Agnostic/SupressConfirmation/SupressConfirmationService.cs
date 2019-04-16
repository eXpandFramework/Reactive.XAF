using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.SupressConfirmation{
    public static class SupressConfirmationService{
        public const ModificationsHandlingMode ModificationsHandlingMode = (ModificationsHandlingMode) (-1);

        internal static IObservable<Unit> Connect(){
            return Windows
                .Select(DisableWebControllers).ToUnit()
                .Merge(Windows.Select(ChangeModificationHandlingMode).Merge())
                .TakeUntilDisposingMainWindow().ToUnit();
        }

        private static IObservable<Frame> DisableWebControllers(Frame window){
            var strings = new[]{"ASPxGridListEditorConfirmUnsavedChangesController","WebConfirmUnsavedChangesDetailViewController"};
            return window.Controllers.Cast<Controller>()
                .Where(controller => strings.Any(name => controller.GetType().Name == name))
                .Select(controller => {
                    controller.Active[SupressConfirmationModule.CategoryName] = false;
                    return window;
                }).ToArray().ToObservable();
        }

        private static IObservable<Unit> ChangeModificationHandlingMode(Frame window){
            void ChangeMode(){
                window.GetController<ModificationsController>().ModificationsHandlingMode = ModificationsHandlingMode;
            }
            ChangeMode();
            var whenNewDetailViewObjectChangedOnce=Observable.Empty<Unit>();
            if (window.View is DetailView detailView&&detailView.ObjectSpace.IsNewObject(detailView.CurrentObject)){    
                whenNewDetailViewObjectChangedOnce = detailView.ObjectSpace.WhenObjectChanged().FirstAsync().ToUnit();
            }
            return whenNewDetailViewObjectChangedOnce
                .Merge(((ObjectView) window.View).ObjectSpace.WhenCommited().ToUnit())
                .Select(_ => {
                    ChangeMode();
                    return Unit.Default;
                });
        }

        public static IObservable<Frame> Windows{ get; } = RxApp.Windows.Cast<Frame>()

            .Merge(RxApp.NestedFrames.Cast<Frame>())
            .WhenModule(typeof(SupressConfirmationModule))
            .ViewChanged()
            .Where(_ => ((IModelObjectViewSupressConfirmation) _.frame.View.Model).SupressConfirmation)
            .Select(_ => _.frame).Publish().RefCount();

    }
}