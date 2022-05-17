using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using ModelEditor;
using Task = System.Threading.Tasks.Task;

namespace XVSIX64 {
	
	internal sealed class ModelEditorCommand {
		public const int CommandId = 0x0100;
		public static readonly Guid CommandSet = new Guid("32dec059-e0ad-40a1-a5b1-8bebd1171a00");
		private readonly AsyncPackage _package;

		private ModelEditorCommand(AsyncPackage package, OleMenuCommandService commandService) {
			this._package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(this.ExecuteAsync, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		public static async Task InitializeAsync(AsyncPackage package) {
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
			Instance = new ModelEditorCommand(package, commandService);
		}

		
		public static ModelEditorCommand Instance {
			get;
			private set;
		}

		public IAsyncServiceProvider ServiceProvider => _package;

		public static async Task InitializeAsync(XVSIX64Package package) {
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
