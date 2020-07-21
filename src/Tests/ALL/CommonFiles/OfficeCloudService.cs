using DevExpress.EasyTest.Framework;
using Xpand.Extensions.Office.Cloud;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;

namespace ALL.Win.Tests{
    public static class OfficeCloudService{
        private static void CheckOperatrion(this ICommandAdapter commandAdapter, MapAction mapAction, string verifyEditor){
            commandAdapter.Execute(new WaitCommand(2000), new ActionCommand(Actions.Refresh));
            commandAdapter.Execute(new CheckDetailViewCommand((verifyEditor, mapAction.ToString())));
        }

        public static void TestOfficeCloudService(this ICommandAdapter commandAdapter, string navigationItemCaption,
            string editorName, string verifyEditor){
            commandAdapter.Execute(new NavigateCommand(navigationItemCaption),new ActionCommand(Actions.New));
            commandAdapter.Execute(new FillObjectViewCommand((editorName, "New")),
                new ActionCommand(Actions.Save));
            
            commandAdapter.CheckOperatrion(MapAction.Insert, verifyEditor);
                    
            commandAdapter.Execute(new FillObjectViewCommand((editorName, "Update")),new ActionCommand(Actions.Save));
            commandAdapter.CheckOperatrion(MapAction.Update, verifyEditor);

            commandAdapter.Execute(new ActionDeleteCommand());
            
            
            commandAdapter.Execute(new NavigateCommand(navigationItemCaption));
            commandAdapter.Execute(new ActionCommand(Actions.Refresh), new CheckListViewCommand("",0));
        }

    }
}