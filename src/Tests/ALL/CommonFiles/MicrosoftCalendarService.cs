using ALL.Tests;
using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;

namespace ALL.Win.Tests{
    public static class MicrosoftCalendarService{
        public static void TestMicrosoftCalendarService(this ICommandAdapter commandAdapter){
            commandAdapter.TestOfficeCloudService("Cloud.Microsoft Event", nameof(Event.Subject),
                nameof(Event.Description));
            commandAdapter.Execute(new NavigateCommand("Cloud.Microsoft Event"));
        
            commandAdapter.Execute(new ActionCommand("Cloud Operation","New"));

            commandAdapter.Execute(new WaitCommand(5000),new ActionCommand(Actions.Refresh));
            var checkListViewCommand = new CheckListViewCommand("Subject");
            checkListViewCommand.AddRows(new[]{"Cloud"});
            commandAdapter.Execute(checkListViewCommand);

            commandAdapter.Execute(new ActionCommand("Cloud Operation","Update"));

            commandAdapter.Execute(new WaitCommand(5000),new ActionCommand(Actions.Refresh));
            checkListViewCommand = new CheckListViewCommand("Subject");
            checkListViewCommand.AddRows(new[]{"Cloud-Updated"});
            commandAdapter.Execute(checkListViewCommand);

            commandAdapter.Execute(new ActionCommand("Cloud Operation","Delete"));
            commandAdapter.Execute(new WaitCommand(5000),new ActionCommand(Actions.Refresh));
            commandAdapter.Execute(new CheckListViewCommand("",0));
        }
    }
}