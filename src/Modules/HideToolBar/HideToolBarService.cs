using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Templates;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.HideToolBar{
    public static class HideToolBarService{
        public static IObservable<Frame> HideToolBarNestedFrames(this XafApplication application){
            return application
                .WhenNestedFrameCreated()
                .Select(frame => frame)
                .TemplateChanged()
                .Where(_ => _.Template is ISupportActionsToolbarVisibility)
                .TemplateViewChanged()
                .Where(frame => {
                    if (frame.View is ListView&& frame.View.Model is IModelListViewHideToolBar modelListViewHideToolBar){
                        var hideToolBar = modelListViewHideToolBar.HideToolBar;
                        return hideToolBar.HasValue&&hideToolBar.Value;
                    }

                    return false;
                })
                .Publish().RefCount();
        }

        internal static IObservable<Unit> Connect(this XafApplication application){
            if (application != null){
                return application.HideToolBarNestedFrames()
                    .Do(HideToolBar)
                    .ToUnit();
            }
            return Observable.Empty<Unit>();
        }

        public static void HideToolBar(this Frame frame){
            if (frame.Application.GetPlatform()==Platform.Win){
                var toolbarVisibilityController = frame.Controllers.Cast<Controller>().FirstOrDefault(controller =>
                    controller.Name == "DevExpress.ExpressApp.Win.SystemModule.ToolbarVisibilityController");
                if (toolbarVisibilityController != null){
                    toolbarVisibilityController.Active[HideToolBarModule.CategoryName] = false;
                }
            }
            var visibility = frame.Template as ISupportActionsToolbarVisibility;
            visibility?.SetVisible(false);
        }
    }
}