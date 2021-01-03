using DevExpress.EasyTest.Framework;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.TestsLib.Common.BO;
using Xpand.TestsLib.Common.EasyTest;
using Xpand.TestsLib.Common.EasyTest.Commands;
using Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands;

namespace ALL.Tests{
	public static class ViewWizardService{
        public static void TestViewWizardService(this ICommandAdapter commandAdapter){
            commandAdapter.Execute(new NavigateCommand("ViewWizard.Order"));
            commandAdapter.Execute(new ActionCommand(Actions.New));
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.OrderID).CompoundName(),"10"));
            commandAdapter.Execute(new ActionCommand(Actions.Save));
            // commandAdapter.Execute(new ActionCommand(nameof(Xpand.XAF.Modules.ViewWizard.ViewWizardService.ShowWizard).CompoundName()));
            // commandAdapter.Execute(new CheckDetailViewCommand((nameof(Order.OrderID).CompoundName(), "10")));
            // commandAdapter.Execute(new ActionCommand(nameof(Xpand.XAF.Modules.ViewWizard.ViewWizardService.NextWizardView).CompoundName()));
            // commandAdapter.Execute(new ActionCommand(new FillEditorCommand(nameof(Order.OrderID).CompoundName(),"10"){ExpectException = true}.CompoundName()));

        }
    }
}