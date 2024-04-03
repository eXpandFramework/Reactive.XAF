using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Xpand.Extensions.IntPtrExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.ModelEditor.Module.Win.BusinessObjects;
using Xpand.XAF.Modules.OneView;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.ModelEditor.Module.Win {
    internal static class ModuleExtensions {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        public static IObservable<XafApplication> ShowModelListView(this XafApplication application)
            => application.WhenSetupComplete()
                .SelectMany(_ => application.WhenWindowCreated(true).To(application))
                .TraceModelEditorWindowsFormsModule()
                .Select(_ => {
                    var synchronizationContext = SynchronizationContext.Current;
                    var selectMany = MEService.WhenMESettings()
                        .SelectMany(solutionPath => application.ShowOneViewParameters()
                            .ParseProjects(solutionPath)
                            .ShowOneView()
                            .Delay(TimeSpan.FromMilliseconds(100))
                            .ObserveOn(synchronizationContext!)
                            .Do(parameters => {
                                parameters.CreatedView.ObjectSpace.Refresh();
                                ((Form)parameters.Controllers.OfType<OneViewDialogController>().First().Frame.Template).Handle.ForceWindowToForeGround();
                            })
                            .SelectMany(parameters => parameters.CloseViewWhenNotForeground(synchronizationContext))
                            .TraceModelEditorWindowsFormsModule(TradeParameters)
                            .EditModel());
                    return selectMany;
                }).Switch().IgnoreElements()
                .To(application).StartWith(application);

        private static IObservable<ShowViewParameters> CloseViewWhenNotForeground(this ShowViewParameters parameters,SynchronizationContext synchronizationContext) 
            => Observable.Interval(TimeSpan.FromSeconds(1))
                .ObserveOn(synchronizationContext)
                .SelectMany(_ => parameters.Controllers.OfType<OneViewDialogController>()
                    .Select(controller => controller.Frame).ToObservable().WhenNotDefault()
                    .Do(frame => {
                        var intPtr = ((Form)frame.Template).Handle;
                        var foregroundWindow = GetForegroundWindow();
                        if (foregroundWindow != intPtr&&intPtr!=IntPtr.Zero) {
                            parameters.CreatedView.Close();
                        }
                    }))
                .To(parameters).StartWith(parameters);

        private static IObservable<ShowViewParameters> ParseProjects(this IObservable<ShowViewParameters> source, string solutionPath)
            => source.MergeIgnored(parameters => parameters.CreatedView.ObjectSpace.AsNonPersistentObjectSpace()
                .WhenObjects(t1 => Unit.Default.Observe()
	                .Do(_ => parameters.CreatedView.AsObjectView().Application().ShowViewStrategy.ShowMessage(nameof(ParseProjects)))
	                .SelectMany(_ => SolutionFile.Parse(solutionPath).Projects().Models(t1.objectSpace))
                    .TraceModelEditorWindowsFormsModule(model => model.Name))
                .Do(_ => MEService.DeleteMESettings(null)));

        private static string TradeParameters(this ShowViewParameters parameters) => parameters.CreatedView.Id;

        public static IObservable<Unit> CloseViewWhenNotSettings(this XafApplication application, string meInstallationPath) 
            => application.WhenViewShown()
                .WhenDefault(_ => File.Exists(MEService.GetMESettingsPath()))
                .Do(t => t.TargetFrame.View.Close())
                .ToUnit();

        private static IEnumerable<XafModel> Models(this IEnumerable<(MSBuildProject msBuildProject, Project project)> source, IObjectSpace objectSpace)
            => source.SelectMany(t => new[] {"None", "Content", "EmbeddedResource"}
                .SelectMany(t.project.GetItems)
                .Where(item => item.EvaluatedInclude.EndsWith(".xafml"))
                .Select(item => {
                    var xafModel = objectSpace.CreateObject<XafModel>();
                    xafModel.Project = t.msBuildProject;
                    xafModel.Name =
                        $"{Path.GetFileNameWithoutExtension(xafModel.Project.Path)}\\{item.EvaluatedInclude.Replace(ModelStoreBase.ModelDiffDefaultName, "").Replace(".xafml","")}";
                    xafModel.Name = xafModel.Name.TrimEnd('\\');
                    xafModel.Path = $"{Path.GetDirectoryName(xafModel.Project.Path)}\\{item.EvaluatedInclude}";
                    objectSpace.CommitChanges();
                    return xafModel;
                })
            );

        internal static IObservable<TSource> TraceModelEditorWindowsFormsModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, ModelEditorWindowsFormsModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName:memberName);
    }
}