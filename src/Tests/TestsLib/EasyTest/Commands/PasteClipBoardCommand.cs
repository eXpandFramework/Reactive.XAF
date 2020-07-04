using System.Windows.Forms;
using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands{
	public class PasteClipBoardCommand:EasyTestCommand{
		public const string Name = "PasteClipBoard";

		public PasteClipBoardCommand(object value){
			Clipboard.SetText(value.ToString());
		}

		protected override void ExecuteCore(ICommandAdapter adapter){
			var sendKeysCommand = new SendKeysCommand("^{v}");
			sendKeysCommand.Execute(adapter);
		}
	}
}