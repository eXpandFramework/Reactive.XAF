using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Xpand.Extensions.IntPtrExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.XAF.ModelEditor.Module.Win.BusinessObjects;
using Xpand.XAF.Modules.OneView;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.ModelEditor.Module.Win {
    internal static class ModuleExtensions {
        
        public static IObservable<XafApplication> ShowModelListView(this XafApplication application)
            => application.WhenSetupComplete()
                .SelectMany(_ => application.WhenWindowCreated(true))
                .Select(_ => SynchronizationContext.Current)
                .TraceModelEditorWindowsFormsModule()
                .Select(synchronizationContext =>MEService.WhenMESettings().ToConsole(_ => "SolutionPath")
                    .Publish(source => source.ShowModelListView(application, synchronizationContext)) 
                ).Switch().IgnoreElements()
                .To(application).StartWith(application);

        private static IObservable<ShowViewParameters> ShowModelListView(this IObservable<string> source,XafApplication application,SynchronizationContext synchronizationContext) 
            => source.SelectMany(solutionPath => application.ShowModelListView( synchronizationContext,solutionPath));

        
        private static IObservable<ShowViewParameters> ShowModelListView(this XafApplication application,
            SynchronizationContext synchronizationContext, string solutionPath)
            => application.ShowOneViewParameters()
                .ShowOneView().ToConsole(_ => "ShowOneView")
                .Do(parameters => {
                    MEService.DeleteMESettings(null);
                    var form = ((Form)parameters.Controllers.OfType<OneViewDialogController>().First().Frame.Template);
                    form.TopMost = true;
                    form.Handle.ForceWindowToForeGround();
                })
                .ObserveOnDefault()
                .SelectMany(parameters => application.ParseProjects( synchronizationContext, solutionPath, parameters))
                .TraceModelEditorWindowsFormsModule(TradeParameters);

        private static IObservable<ShowViewParameters> ParseProjects(this XafApplication application, SynchronizationContext synchronizationContext, string solutionPath, ShowViewParameters parameters) 
            => application.CreateObjectSpace()
                .Use(objectSpace => {
                    var activeConfiguration = ((IModelApplicationME)application.Model).ModelEditor.ActiveConfiguration;
                    var models = objectSpace.GetObjectsQuery<XafModel>().ToArray();
                    return SolutionFile.Parse(solutionPath).Projects(objectSpace,activeConfiguration).Models(objectSpace).ToNowObservable()
                        .BufferUntilCompleted().Do(_ => {
                            objectSpace.Delete(models);
                            objectSpace.CommitChanges();
                        });
                }).BufferUntilCompleted().To(parameters).ToConsole()
                .TakeUntil(parameters.CreatedView.WhenClosing().ToConsole(_ => "Closed"))
                .ObserveOnContext(synchronizationContext)
                .Do(viewParameters => viewParameters.CreatedView.ObjectSpace.Refresh());
        
        private static string TradeParameters(this ShowViewParameters parameters) => parameters.CreatedView.Id;

        public static IObservable<Unit> CloseViewWhenNotSettings(this XafApplication application) 
            => application.WhenViewShown()
                .WhenDefault(_ => File.Exists(MEService.GetMESettingsPath()))
                .Do(t => t.TargetFrame.View.Close())
                .ToUnit();

        private static IEnumerable<XafModel> Models(this IEnumerable<(MSBuildProject msBuildProject, Project project)> source, IObjectSpace objectSpace)
            => source.SelectMany(t => new[] {"None", "Content", "EmbeddedResource"}
                .SelectMany(t.project.GetItems)
                .Where(item => item.EvaluatedInclude.EndsWith(".xafml"))
                .DistinctBy(item => $"{Path.GetDirectoryName(t.msBuildProject.Path)}\\{item.EvaluatedInclude}")
                .Select(item => {
                    var xafModel = objectSpace.CreateObject<XafModel>();
                    xafModel.Project = t.msBuildProject;
                    xafModel.Name = $"{Path.GetFileNameWithoutExtension(xafModel.Project.Path)}\\{item.EvaluatedInclude.Replace(ModelStoreBase.ModelDiffDefaultName, "").Replace(".xafml","")}";
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