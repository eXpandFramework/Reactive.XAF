using System.Linq;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.BO;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using Xpand.TestsLib.EasyTest.Commands.DialogCommands;
using ActionCommand = Xpand.TestsLib.EasyTest.Commands.ActionCommands.ActionCommand;
using NavigateCommand = Xpand.TestsLib.EasyTest.Commands.NavigateCommand;

namespace ALL.Web.Tests{

    public static class LookupCascadeService{
        private static readonly NavigateCommand NavigateToOrderListView=new NavigateCommand("LookupCascade.Order");

        public static ICommandAdapter TestLookupCascade(this ICommandAdapter commandAdapter){
            commandAdapter.Execute(NavigateToOrderListView);
            return commandAdapter
                .ListView()
                .DetailView()
                .DetailViewPopup()
                .SaveDetailView()
                .SaveListViewWithSynchronizationOn();
        }

        static ICommandAdapter SaveListViewWithSynchronizationOn(this ICommandAdapter commandAdapter){
	        commandAdapter.Execute(new SelectObjectsCommand(),new ActionDeleteObjectsCommand(),
                new ActionGridListEditorCommand(GridListEditorInlineCommand.New),
                new FillEditorCommand(nameof(Order.Accessory), $"{nameof(Accessory.AccessoryName)}0",true),
                new ActionGridListEditorCommand(GridListEditorInlineCommand.Update)
                ,CheckListViewCommand(1));
            return commandAdapter;
        }

        static ICommandAdapter SaveDetailView(this ICommandAdapter commandAdapter){
	        commandAdapter.Execute(new SelectObjectsCommand(),new ActionDeleteObjectsCommand(),new ActionCommand(Actions.New),
                new FillEditorCommand(nameof(Order.Product), $"{nameof(Product.ProductName)}0"),
                new FillEditorCommand(nameof(Order.Accessory), $"{nameof(Accessory.AccessoryName)}0"),
                new ActionCommand(Actions.SaveAndClose),CheckListViewCommand(1));
            return commandAdapter;
        }

        static ICommandAdapter DetailViewPopup(this ICommandAdapter commandAdapter){
	        commandAdapter.Execute(new  ActionCommand("Show In Popup"));
            commandAdapter.TestDetailView();
            commandAdapter.Execute(new ActionCommand(Actions.Cancel));
            commandAdapter.Execute(new RespondDialogCommand("OK"));
            commandAdapter.Execute(NavigateToOrderListView);
            return commandAdapter;
        }

        static ICommandAdapter DetailView(this ICommandAdapter commandAdapter){
	        commandAdapter.Execute(
                new ActionEditObjectCommand(nameof(Order.Product),$"{nameof(Product.ProductName)}0"),
                new CheckDetailViewCommand((nameof(Order.Product),$"{nameof(Product.ProductName)}0")),
                NavigateToOrderListView,new RespondDialogCommand("OK"),new ActionEditObjectCommand(nameof(Order.Product), $"{nameof(Product.ProductName)}0"));
            commandAdapter.TestDetailView();
            commandAdapter.Execute(NavigateToOrderListView);
            commandAdapter.Execute(new RespondDialogCommand("OK"));
            return commandAdapter;
        }

        static void TestDetailView(this ICommandAdapter commandAdapter){
            commandAdapter.Execute(new CheckDetailViewCommand((nameof(Order.Product), $"{nameof(Product.ProductName)}0")));
            commandAdapter.TestEditors();
            commandAdapter.TestListView(1);
        }

        static void TestEditors(this ICommandAdapter commandAdapter, bool inlineEditor=false){
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Product), Xpand.XAF.Modules.LookupCascade.LookupCascadeService.NA,inlineEditor));
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Accessory), Xpand.XAF.Modules.LookupCascade.LookupCascadeService.NA,inlineEditor));
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Accessory), $"{nameof(Accessory.AccessoryName)}0",inlineEditor));
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Product), $"{nameof(Product.ProductName)}0",inlineEditor));
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Accessory), $"{nameof(Accessory.AccessoryName)}0",inlineEditor));
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Accessory), $"{nameof(Accessory.AccessoryName)}1",inlineEditor){ExpectException = true});
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Product), $"{nameof(Product.ProductName)}1",inlineEditor));
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Accessory), $"{nameof(Accessory.AccessoryName)}0",inlineEditor){ExpectException = true});
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Accessory), $"{nameof(Accessory.AccessoryName)}1",inlineEditor));
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Product), $"{nameof(Product.ProductName)}2",inlineEditor){ExpectException = true});
            commandAdapter.Execute(new FillEditorCommand(nameof(Order.Accessory), $"{nameof(Accessory.AccessoryName)}2",inlineEditor){ExpectException = true});
        }

        static ICommandAdapter ListView(this ICommandAdapter commandAdapter){
	        commandAdapter.TestListView(2);
            commandAdapter.Execute(NavigateToOrderListView);
            commandAdapter.Execute(new RespondDialogCommand("OK"));
            return commandAdapter;
        }

        private static void TestListView(this ICommandAdapter commandAdapter,int rowCount){
            var checkListViewCommand = CheckListViewCommand(rowCount);
            commandAdapter.Execute(checkListViewCommand);
            commandAdapter.Execute(new ActionGridListEditorCommand((nameof(Order.Product), $"{nameof(Product.ProductName)}0")));
            commandAdapter.TestEditors(true);
            commandAdapter.Execute(new ActionGridListEditorCommand(GridListEditorInlineCommand.Cancel));
            commandAdapter.Execute(new ActionGridListEditorCommand(GridListEditorInlineCommand.New));
            commandAdapter.TestEditors(true);
            
        }

        private static CheckListViewCommand CheckListViewCommand(int rowCount){
            var checkListViewCommand = new CheckListViewCommand(nameof(Order.Product), nameof(Order.Accessory));
            checkListViewCommand.AddRows(Enumerable.Range(0, rowCount)
                .Select(i => new[]{$"{nameof(Product.ProductName)}{i}", $"{nameof(Accessory.AccessoryName)}{i}"}).ToArray());
            return checkListViewCommand;
        }

    }
}