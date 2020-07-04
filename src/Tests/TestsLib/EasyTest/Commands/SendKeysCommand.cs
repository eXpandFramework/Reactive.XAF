using System.Windows.Forms;
using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands{

	public class SendKeysCommand : EasyTestCommand{
		private readonly string _keys;
		public const string Name = "SendKeys";

		public SendKeysCommand(string keys){
			_keys = keys;
		}

		protected override void ExecuteCore(ICommandAdapter adapter){
			SendKeys.SendWait(_keys);
		}
	}
}