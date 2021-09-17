using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using ModelEditor;
using Task = System.Threading.Tasks.Task;

namespace XVSIX {
    
    internal sealed class ModelEditorCommand {
        
        public const int CommandId = 256;

        public static readonly Guid CommandSet = new("9a3d22df-7081-41e2-a1db-0293adaadd34");

        private readonly AsyncPackage _package;

        private ModelEditorCommand(AsyncPackage package, OleMenuCommandService commandService) {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.ExecuteAsync, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ModelEditorCommand Instance {
            get;
            private set;
        }

        public IAsyncServiceProvider ServiceProvider => _package;

        public static async Task InitializeAsync(XVSIXPackage package) {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ModelEditorCommand(package, commandService);
            await XpandModelEditor.ExtractMEAsync();
            
        }

        private async void ExecuteAsync(object sender, EventArgs e) {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await XpandModelEditor.StartMEAsync();
            XpandModelEditor.WriteSettings(((DTE2) Package.GetGlobalService(typeof(DTE))).Solution.FileName);
        }

        
    }
}
