using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Templates;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.HideToolBar{
    public static class HideToolBarService{
        internal static IObservable<TSource> TraceHideToolBarModule<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0){
            return source.Trace(name, HideToolBarModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }

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
                .TraceHideToolBarModule()
                .Publish().RefCount();
        }

        internal static IObservable<Unit> Connect(this XafApplication application){
            if (application != null){
                return application.HideToolBarNestedFrames()
                    .HideToolBar()
                    .ToUnit();
            }
            return Observable.Empty<Unit>();
        }

        public static IObservable<Frame> HideToolBar(this IObservable<Frame> source){
            return source.Select(frame => {
                if (frame.Application.GetPlatform() == Platform.Win){
                    var toolbarVisibilityController = frame.Controllers.Cast<Controller>().FirstOrDefault(controller =>
                        controller.Name == "DevExpress.ExpressApp.Win.SystemModule.ToolbarVisibilityController");
                    if (toolbarVisibilityController != null){
                        toolbarVisibilityController.Active[HideToolBarModule.CategoryName] = false;
                    }
                }

                var visibility = frame.Template as ISupportActionsToolbarVisibility;
                visibility?.SetVisible(false);
                return frame;
            }).TraceHideToolBarModule();

        }
    }
}