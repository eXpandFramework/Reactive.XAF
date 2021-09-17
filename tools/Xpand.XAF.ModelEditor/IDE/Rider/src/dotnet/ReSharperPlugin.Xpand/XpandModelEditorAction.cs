
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.ReSharper.Psi.Files;
using ModelEditor;

namespace ReSharperPlugin.Xpand{
    [Action("XpandModelEditorAction", "Xpand ModelEditor")]
    public class XpandModelEditorAction : IActionWithExecuteRequirement, IExecutableAction{
        public IActionRequirement GetRequirement(IDataContext dataContext) 
            => CommitAllDocumentsRequirement.TryGetInstance(dataContext);

        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate) 
            => true;

        public async void Execute(IDataContext context, DelegateExecute nextExecute){
            await XpandModelEditor.ExtractMEAsync();
            await XpandModelEditor.StartMEAsync();
            var solution = context.GetData(JetBrains.ProjectModel.DataContext.ProjectModelDataConstants.SOLUTION);
            XpandModelEditor.WriteSettings(solution?.SolutionFilePath.FullPath);
        }
    }
}