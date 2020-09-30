using System.Drawing;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using Xpand.TestsLib.EasyTest.Commands.Automation;
using Xpand.TestsLib.EasyTest.Commands.DialogCommands;
using Xpand.TestsLib.Win32;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using ActionCommand = Xpand.TestsLib.EasyTest.Commands.ActionCommands.ActionCommand;
using ProcessRecordCommand = Xpand.TestsLib.EasyTest.Commands.ProcessRecordCommand;

namespace ALL.Win.Tests{
    public static class DocumentStyleManagerService{
        public static async Task TestDocumentStyleManager(this ICommandAdapter adapter){
            adapter.Execute(new NavigateCommand("DocumentStyleManager.Document Object".CompoundName()));
            adapter.Execute(new ProcessRecordCommand("DocumentObject",("Name", "Test document 1")));
            adapter.Execute(new ActionCommand("StyleManager"));
            adapter.Execute(new MoveWindowCommand());
            await adapter.TestAllStyles();
            adapter.TestDeleteStyles();
            adapter.TestImportStyles();
            await adapter.TestReplaceStyles();
            adapter.TestTemplateStyles();
            adapter.TestApplyStyles();
        }

        private static void TestApplyStyles(this ICommandAdapter adapter){
            adapter.Execute(new NavigateCommand("DocumentStyleManager.Document Object".CompoundName()));
            adapter.Execute(new SelectObjectsCommand("DocumentObject", "Name", "Test document 1"));
            adapter.Execute(new SelectObjectsCommand("DocumentObject", "Name", "Test document 2"));
            adapter.Execute(new ActionCommand("Apply Styles"));
            adapter.Execute(new FillObjectViewCommand<ApplyTemplateStyle>((style => style.Template,"Template")));
            adapter.Execute(new ActionCommand("Save Changes"));
            adapter.Execute(new ActionCommand("Save Changes"));
            adapter.Execute(new ActionCommand(Actions.Close));
        }

        private static void TestTemplateStyles(this ICommandAdapter adapter){
            adapter.Execute(new ActionCommand("Linked styles template"));
            adapter.Execute(new EditorActionNewCommand<DocumentStyleManager>(m => m.DocumentStyleLinkTemplate));
            adapter.Execute(new FillObjectViewCommand<DocumentStyleLinkTemplate>((template => template.Name,"Template")));
            adapter.Execute(new ActionCommand(Actions.SaveAndClose));
            adapter.Execute(new ProcessRecordCommand<DocumentStyleManager,DocumentStyle>(manager => manager.ReplacementStyles, (style => style.StyleName,"QuoteV2")){SuppressExceptions = true});
            adapter.Execute(new ActionCommand("Template Styles"));
            adapter.Execute(new CheckListViewSelectionCommand("Document Style Links", (nameof(DocumentStyleLink.Original), "Quote")));
            adapter.Execute(new CheckListViewSelectionCommand("Document Style Links", (nameof(DocumentStyleLink.Replacement), "QuoteV2")));
            adapter.Execute(new ActionCommand("Accept"));
            adapter.Execute(new ActionCommand(Actions.Cancel));
            adapter.Execute(new CloseDialogCommand());
            adapter.Execute(new ActionCommand(Actions.Close));
        }

        private static async Task TestReplaceStyles(this ICommandAdapter adapter){
            var firstWord = new Point(155, 196);
            var styleV2 = "QuoteV2";
            var styleV1 = "Quote";
            adapter.Execute(new MouseCommand(firstWord),new WaitCommand(2000));
            adapter.Execute(new ProcessRecordCommand<DocumentStyleManager>(manager => manager.ReplacementStyles, ("Name", styleV2)){SuppressExceptions = true});
            adapter.Execute(new MouseCommand(new Point(185, 196)), new WaitCommand(2000));
            adapter.Execute(new ProcessRecordCommand<DocumentStyleManager>(manager => manager.ReplacementStyles, ("Name", styleV2)){SuppressExceptions = true});
            adapter.Execute(new ActionCommand(nameof(ReplaceStylesService.ReplaceStyles)));
            await adapter.Execute(() => adapter.Execute(new CheckListViewSelectionCommand<DocumentStyleManager>(m => m.AllStyles, ("Name", styleV2))));
            adapter.Execute(new MouseCommand(firstWord), new WaitCommand(2000));
            adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Z, Win32Constants.VirtualKeys.Control));
            await adapter.Execute(() => adapter.Execute(new CheckListViewSelectionCommand<DocumentStyleManager>(m => m.AllStyles, ("Name", styleV1))));

        }


        private static async Task TestAllStyles(this ICommandAdapter adapter){
            await adapter.TestAllStyleContentSynchronization();
            await adapter.TestAllStylesCharacter();
        }

        private static async Task TestAllStyleContentSynchronization(this ICommandAdapter adapter){
            adapter.Execute(new CheckListViewCommand<DocumentStyleManager>(m => m.AllStyles, 9));

            adapter.Execute(new CheckListViewSelectionCommand<DocumentStyleManager>(m => m.AllStyles, ("Name", "Quote")));

            adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.PageDown));
            adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.PageDown));
            await adapter.Execute(() => {
                adapter.Execute(
                    new CheckListViewSelectionCommand<DocumentStyleManager>(m => m.AllStyles, ("Name", "Intense Quote")));
            });

            adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.PageDown));
            adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.PageDown));
            await adapter.Execute(() => {
                adapter.Execute(new CheckListViewSelectionCommand<DocumentStyleManager>(m => m.AllStyles, ("Name", "Normal")));
            });
            adapter.Execute(new CheckListViewCommand<DocumentStyleManager>(m => m.ReplacementStyles, 3));
        }

        private static async Task TestAllStylesCharacter(this ICommandAdapter adapter){
            adapter.Execute(6, new SendKeysCommand(Win32Constants.VirtualKeys.Up));
            await adapter.Execute(() => adapter.Execute(new CheckListViewSelectionCommand<DocumentStyleManager>(m => m.AllStyles, ("Name", "Hyperlink"))));
            adapter.Execute(new CheckListViewCommand<DocumentStyleManager>(m => m.ReplacementStyles, 6));
        }

        private static void TestImportStyles(this ICommandAdapter adapter){
            adapter.Execute(new ActionCommand(nameof(DeleteService.DeleteStyles), "Unused"));
            adapter.Execute(new ActionCommand("Import"));
            adapter.Execute(new CheckListViewCommand<DocumentStyle>(7));
            adapter.Execute(new SelectObjectsCommand<DocumentStyle>(style => style.StyleName,new[]{"QuoteV2","Intense Quote V2","Hyperlink V2"} ));
            adapter.Execute(new ActionCommand(Actions.OK));
            adapter.Execute(() => adapter.Execute(new CheckListViewCommand<DocumentStyleManager>(m => m.AllStyles, 9)));
        }

        private static void TestDeleteStyles(this ICommandAdapter adapter){
            adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Home,Win32Constants.VirtualKeys.Control));
            adapter.Execute(new ActionCommand(nameof(DeleteService.DeleteStyles)));
            adapter.Execute(new CheckListViewCommand<DocumentStyleManager>(m => m.AllStyles, 8));
            adapter.Execute(new CheckListViewSelectionCommand<DocumentStyleManager>(m => m.AllStyles, ("Name", "Quote")){ExpectException = true});
            adapter.Execute(new CheckListViewSelectionCommand<DocumentStyleManager>(m =>m.AllStyles , ("Name", "Normal")));

            adapter.Execute(new ActionCommand(nameof(DeleteService.DeleteStyles), "Unused"));
            adapter.Execute(new CheckListViewCommand<DocumentStyleManager>(m => m.AllStyles, 5));

            adapter.Execute(new ActionCommand(Actions.Cancel));
            adapter.Execute(new ActionCommand("StyleManager"));
        }
    }
}
