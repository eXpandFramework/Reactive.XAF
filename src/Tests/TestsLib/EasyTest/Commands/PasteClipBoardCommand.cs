using System.Windows.Forms;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.Win32;

namespace Xpand.TestsLib.EasyTest.Commands{
	public class PasteClipBoardCommand:EasyTestCommand{
		public const string Name = "PasteClipBoard";

		public PasteClipBoardCommand(object value){
			Clipboard.SetText(value.ToString());
		}

		protected override void ExecuteCore(ICommandAdapter adapter){
			var inputSimulator = new InputSimulator.InputSimulator();
			inputSimulator.Keyboard.TextEntry(Clipboard.GetText());
		}
	}
}