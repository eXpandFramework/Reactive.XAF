using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.Common.BO;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;

namespace Web.Tests {
    static class TestExtensions {
        public static void DeleteAllProducts(this ICommandAdapter adapter) {
            adapter.Execute(new SelectObjectsCommand<Product>(product => product.ProductName,new []{"ProductName0","ProductName1"}));
            adapter.Execute(new ActionDeleteCommand());
        }

    }
}