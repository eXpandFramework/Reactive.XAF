using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;
using Xpand.TestsLib.Common.EasyTest;
using Xpand.TestsLib.Common.EasyTest.Commands;
using Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands;

namespace Win.Tests {
    public static class ModelViewInheritanceService {
        public static void TestModelViewInheritance(this ICommandAdapter adapter) {
            adapter.Execute(new NavigateCommand("Default.Model View Inheritance"),new ActionCommand(Actions.New));
            
            adapter.Execute(new FillEditorCommand(nameof(Person.FirstName),"test"){ExpectException = true});
            adapter.Execute(new FillEditorCommand(nameof(Person.LastName),"test"));
        }

    }
}
