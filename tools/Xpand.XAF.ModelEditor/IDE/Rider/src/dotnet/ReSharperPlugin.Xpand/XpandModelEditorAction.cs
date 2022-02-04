
using System;
using System.IO;
using JetBrains.Application.DataContext;
using JetBrains.Application.Notifications;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
using ModelEditor;

namespace ReSharperPlugin.Xpand{
    
    [Action("XpandModelEditorAction", "Xpand ModelEditor")]
    public class XpandModelEditorAction : IActionWithExecuteRequirement, IExecutableAction{
        public IActionRequirement GetRequirement(IDataContext dataContext) 
            => CommitAllDocumentsRequirement.TryGetInstance(dataContext);

        bool IExecutableAction.Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate) 
            => true;

        public async void Execute(IDataContext context, DelegateExecute nextExecute){
            var solution = context.GetData(JetBrains.ProjectModel.DataContext.ProjectModelDataConstants.SOLUTION);
            var solutionFileName = solution?.SolutionFilePath.FullPath;
            
            var notifications = context.GetComponent<UserNotifications>();
            try{
                Notify(notifications,nameof(XpandModelEditor.ExtractMEAsync));
                await XpandModelEditor.ExtractMEAsync();
                Notify(notifications,nameof(XpandModelEditor.StartMEAsync));
                await XpandModelEditor.StartMEAsync();
            }
            catch (Exception e){
                Log.Root.Log(LoggingLevel.ERROR, e.ToString());
                Notify(notifications,e.ToString());
                throw;
            }

            if (!File.Exists(solutionFileName)){
                MessageBox.ShowInfo($"Canot find solution file {solutionFileName}");
            }
            Notify(notifications,nameof(XpandModelEditor.WriteSettings));
            XpandModelEditor.WriteSettings(solutionFileName);
        }

        private static void Notify(UserNotifications notifications,string message){
            notifications.CreateNotification(Lifetime.Eternal, TimeSpan.FromSeconds(3), body: message,
                title: "ModelEditor");
        }
    }
}


