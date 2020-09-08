using System.Collections.Generic;
using System.Linq;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.Win32;

namespace Xpand.TestsLib.EasyTest.Commands.Automation{
	public class SendKeysCommand : EasyTestCommand{
		private readonly Win32Constants.VirtualKeys _keyCodes;
		private readonly IEnumerable<Win32Constants.VirtualKeys> _modifierKeyCodes;

		public SendKeysCommand( Win32Constants.VirtualKeys keyCodes,params Win32Constants.VirtualKeys[] modifierKeyCodes){
			_keyCodes = keyCodes;
			_modifierKeyCodes = modifierKeyCodes;
		}

		protected override void ExecuteCore(ICommandAdapter adapter){
			var inputSimulator = new InputSimulator.InputSimulator();
			if (_modifierKeyCodes.Any()){
				inputSimulator.Keyboard.ModifiedKeyStroke(_modifierKeyCodes, _keyCodes);
			}
			else{
				inputSimulator.Keyboard.KeyPress(_keyCodes);	
			}
			adapter.Execute(new WaitCommand(100));
		}
	}
}