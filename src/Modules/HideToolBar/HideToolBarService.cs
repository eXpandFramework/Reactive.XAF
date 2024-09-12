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
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.HideToolBar{
    public static class HideToolBarService{
        internal static IObservable<TSource> TraceHideToolBarModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, HideToolBarModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);


        public static IObservable<Frame> HideToolBarNestedFrames(this XafApplication application) 
            => application.WhenFrame(ViewType.DetailView)
                .SelectMany(frame => frame.NestedListViews().Where(editor => editor.Frame is NestedFrame)
                    .Where(editor => editor.Frame.ToNestedFrame().HideToolBar()))
                .Select(editor => editor.Frame);

        private static bool HideToolBar(this NestedFrame frame) 
            => frame.Template is ISupportActionsToolbarVisibility&&frame.View is ListView && frame.View.Model is IModelListViewHideToolBar { HideToolBar: { } } modelListViewHideToolBar && modelListViewHideToolBar.HideToolBar.Value;

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.HideToolBarNestedFrames().HideToolBar().ToUnit());

        public static IObservable<Frame> HideToolBar(this IObservable<Frame> source) 
            => source.SelectMany(frame => frame.View.WhenControlsCreated(true)
                .Do(_ => {
                    if (frame.Application.GetPlatform() == Platform.Win) {
                        var toolbarVisibilityController =
                            frame.GetController(
                                "DevExpress.ExpressApp.Win.SystemModule.ToolbarVisibilityController");
                        if (toolbarVisibilityController != null) {
                            toolbarVisibilityController.Active[HideToolBarModule.CategoryName] = false;
                        }

                        var barManager = frame.Template.GetType().Properties().FirstOrDefault(p
                            => p.Name.Contains("DevExpress.ExpressApp.Win.Controls.IBarManagerHolder.BarManager"));
                        barManager?.GetValue(frame.Template).GetPropertyValue("Bars").CallMethod("Clear");
                    }

                    var visibility = frame.Template as ISupportActionsToolbarVisibility;
                    visibility?.SetVisible(false);
                }).To(frame)).TraceHideToolBarModule(frame => frame.View?.Id);
    }
}