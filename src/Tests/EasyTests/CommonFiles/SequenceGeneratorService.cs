using DevExpress.EasyTest.Framework;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.TestsLib.Common.BO;
using Xpand.TestsLib.Common.EasyTest;
using Xpand.TestsLib.Common.EasyTest.Commands;
using Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands;
using Xpand.XAF.Modules.SequenceGenerator;

namespace ALL.Win.Tests{
    public static class SequenceGeneratorService{


        public static void TestSequenceGeneratorService(this ICommandAdapter commandAdapter){
            commandAdapter.Execute(new NavigateCommand("Default.Sequence Storage"));
            commandAdapter.Execute(new SelectObjectsCommand(new MainParameter(nameof(SequenceStorage).CompoundName())){SuppressExceptions = true});
            commandAdapter.Execute(new ActionDeleteObjectsCommand(){SuppressExceptions = true});
            commandAdapter.Execute(new ActionCommand(Actions.New));
            commandAdapter.Execute(new FillEditorCommand(nameof(SequenceStorage.Type),nameof(Order)));
            commandAdapter.Execute(new FillEditorCommand(nameof(SequenceStorage.Member),nameof(Order.OrderID).CompoundName()));
            commandAdapter.Execute(new FillEditorCommand(nameof(SequenceStorage.NextSequence).CompoundName(),"100"));
            commandAdapter.Execute(new ActionCommand(Actions.Save));
            commandAdapter.Execute(new NavigateCommand("Default.Order"));
            commandAdapter.Execute(new ActionCommand(Actions.New));
            commandAdapter.Execute(new ActionCommand(Actions.Save));
            commandAdapter.Execute(new ActionCommand(Actions.Refresh));
            commandAdapter.Execute(new CheckDetailViewCommand((nameof(Order.OrderID).CompoundName(),"100")));
            commandAdapter.Execute(new ActionCommand(Actions.New));
            commandAdapter.Execute(new ActionCommand(Actions.Save));
            commandAdapter.Execute(new ActionCommand(Actions.Refresh));
            commandAdapter.Execute(new CheckDetailViewCommand((nameof(Order.OrderID).CompoundName(),"101")));
        }


        

    }
}