
using System.IO;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
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
            await XpandModelEditor.ExtractMEAsync();
            await XpandModelEditor.StartMEAsync();
            
            
            if (!File.Exists(solutionFileName)){
                MessageBox.ShowInfo($"Canot find solution file {solutionFileName}");
            }
            XpandModelEditor.WriteSettings(solutionFileName);
        }
    }
}