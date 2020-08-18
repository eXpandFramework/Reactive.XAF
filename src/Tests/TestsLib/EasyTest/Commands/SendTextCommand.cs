using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands{
	public class SendTextCommand:EasyTestCommand{
		private readonly string _text;
		

		public SendTextCommand(string text){
			_text = text;
		}

		protected override void ExecuteCore(ICommandAdapter adapter){
			var inputSimulator = new InputSimulator.InputSimulator();
			inputSimulator.Keyboard.TextEntry(_text);
		}
	}
}