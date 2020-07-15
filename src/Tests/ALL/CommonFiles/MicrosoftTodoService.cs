using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.Base.General;
using Xpand.Extensions.Office.Cloud;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using ActionCommand = Xpand.TestsLib.EasyTest.Commands.ActionCommands.ActionCommand;

namespace ALL.Win.Tests{
    public static class MicrosoftTodoService{
        public static void TestMicrosoftTodoService(this ICommandAdapter commandAdapter){
            commandAdapter.Execute(new NavigateCommand("Default.Task"));
            commandAdapter.Execute( new ActionCommand(Actions.New),
                new FillObjectViewCommand((nameof(DevExpress.Persistent.BaseImpl.Task.Subject), "New")),
                new ActionCommand(Actions.Save));
            
            commandAdapter.CheckOperatrion(MapAction.Insert);
                    
            commandAdapter.Execute(new FillObjectViewCommand((nameof(DevExpress.Persistent.BaseImpl.Task.Subject), "Update")),new ActionCommand(Actions.Save));
            commandAdapter.CheckOperatrion(MapAction.Update);

            commandAdapter.Execute(new ActionDeleteCommand());
            
            
            commandAdapter.Execute(new NavigateCommand("Default.Task"));
            commandAdapter.Execute(new ActionCommand(Actions.Refresh), new CheckListViewCommand("Task",0));
        }

        private static void CheckOperatrion(this ICommandAdapter commandAdapter,MapAction mapAction){
            commandAdapter.Execute(new WaitCommand(2000), new ActionCommand(Actions.Refresh));
            commandAdapter.Execute(new CheckDetailViewCommand((nameof(ITask.Description), mapAction.ToString())));
        }
    }
}