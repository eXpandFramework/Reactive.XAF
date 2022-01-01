
using System;
using System.IO;
using JetBrains.Application.DataContext;
using JetBrains.Application.Notifications;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.Diagnostics;
using JetBrains.IDE.UI.Extensions;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Rider.Model.UIAutomation;
using JetBrains.Util;
using JetBrains.Util.Logging;

using ModelEditor;

namespace ReSharperPlugin.Xpand{
    
    [Action("XpandModelEditorAction", "Xpand ModelEditor")]
    public class XpandModelEditorAction : IActionWithExecuteRequirement, IExecutableAction{
        public IActionRequirement GetRequirement(IDataContext dataContext) 
            => CommitAllDocumentsRequirement.TryGetInstance(dataContext);

        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate) 
            => true;

        public async void Execute(IDataContext context, DelegateExecute nextExecute){
            var solution = context.GetData(JetBrains.ProjectModel.DataContext.ProjectModelDataConstants.SOLUTION);
            var solutionFileName = solution?.SolutionFilePath.FullPath;
            // LogEvent.CreateWithMessage(LoggingLevel.ERROR, "aaa", "adsssd");
            // Log.Root.Log(LoggingLevel.ERROR, solutionFileName);
            // Log.Root.Info("asdasd");
            try{
                Log.Root.Log(LoggingLevel.ERROR, "adfsdf");
                await XpandModelEditor.ExtractMEAsync();
                Log.Root.Log(LoggingLevel.INFO, nameof(XpandModelEditor.StartMEAsync));
                await XpandModelEditor.StartMEAsync();
            }
            catch (Exception e){
                Log.Root.Log(LoggingLevel.ERROR, e.ToString());
                throw;
            }

            if (!File.Exists(solutionFileName)){
                MessageBox.ShowInfo($"Canot find solution file {solutionFileName}");
            }
            XpandModelEditor.WriteSettings(solutionFileName);
        }
    }
}