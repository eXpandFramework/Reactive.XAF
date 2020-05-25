using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Templates;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.HideToolBar{
    public static class HideToolBarService{
        internal static IObservable<TSource> TraceHideToolBarModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, HideToolBarModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        public static IObservable<Frame> HideToolBarNestedFrames(this XafApplication application) =>
            application
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
                .TraceHideToolBarModule(frame => $"{frame.ViewItem.View.Id}, {frame.View.Id}")
                .Publish().RefCount();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) =>
            manager.WhenApplication()
                .SelectMany(application =>  application.HideToolBarNestedFrames()
                    .HideToolBar()
                    .Retry(application))
                .ToUnit();

        public static IObservable<Frame> HideToolBar(this IObservable<Frame> source) =>
            source.Select(frame => {
                if (frame.Application.GetPlatform() == Platform.Win){
                    var toolbarVisibilityController = frame.Controllers.Cast<Controller>().FirstOrDefault(controller =>
                        controller.Name == "DevExpress.ExpressApp.Win.SystemModule.ToolbarVisibilityController");
                    if (toolbarVisibilityController != null){
                        toolbarVisibilityController.Active[HideToolBarModule.CategoryName] = false;
                    }
                    var barManager = frame.Template.GetType().Properties().FirstOrDefault(p => p.Name.Contains("DevExpress.ExpressApp.Win.Controls.IBarManagerHolder.BarManager"));
                    barManager?.GetValue(frame.Template).GetPropertyValue("Bars").CallMethod("Clear");
                }
                var visibility = frame.Template as ISupportActionsToolbarVisibility;
                visibility?.SetVisible(false);
                return frame;
            }).TraceHideToolBarModule(frame => frame.View.Id);
    }
}