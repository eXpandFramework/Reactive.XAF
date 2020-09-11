using System;
using System.Reactive;
using System.Reactive.Linq;
using ALL.Tests;
using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using Xpand.TestsLib.EasyTest.Commands.Automation;
using Task = System.Threading.Tasks.Task;

namespace ALL.Win.Tests{
    public static class MicrosoftCalendarService{
        public static IObservable<Unit> TestMicrosoftCalendarService(this ICommandAdapter commandAdapter){
            commandAdapter.TestOfficeCloudService("Cloud.Microsoft Event", nameof(Event.Subject),
                nameof(Event.Description));
            commandAdapter.Execute(new NavigateCommand("Cloud.Microsoft Event"));
            commandAdapter.Execute(new ActionCommand("Cloud Operation","New"));
            return commandAdapter.Execute(() => {
                    commandAdapter.Execute(new ActionCommand(Actions.Refresh));
                    var checkListViewCommand = new CheckListViewCommand("Subject");
                    checkListViewCommand.AddRows(new[]{"Cloud"});
                    commandAdapter.Execute(checkListViewCommand);
                })
                .Do(_ => {
                    commandAdapter.Execute(new ActionCommand("Cloud Operation", "Update"));
                })
                .SelectMany(_ => commandAdapter.Execute(() => {
                    commandAdapter.Execute(new ActionCommand(Actions.Refresh));
                    var checkListViewCommand = new CheckListViewCommand("Subject");
                    checkListViewCommand.AddRows(new[]{"Cloud-Updated"});
                    commandAdapter.Execute(checkListViewCommand);
                })
                .Do(unit => commandAdapter.Execute(new ActionCommand("Cloud Operation","Delete")))
                .Concat(commandAdapter.Execute(() => {
                    commandAdapter.Execute(new ActionCommand(Actions.Refresh));
                    commandAdapter.Execute(new CheckListViewCommand("", 0));
                }).RetryWithBackoff()));
        }
    }
}