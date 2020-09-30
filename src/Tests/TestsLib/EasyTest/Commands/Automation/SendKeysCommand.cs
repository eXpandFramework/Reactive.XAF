using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.InputSimulator;
using Xpand.TestsLib.Win32;

namespace Xpand.TestsLib.EasyTest.Commands.Automation{
	public class SendKeysCommand : EasyTestCommand{
        private readonly Action<IKeyboardSimulator> _action;
        private readonly Win32Constants.VirtualKeys _keyCodes;
		private readonly IEnumerable<Win32Constants.VirtualKeys> _modifierKeyCodes;

		public SendKeysCommand( Win32Constants.VirtualKeys keyCodes,params Win32Constants.VirtualKeys[] modifierKeyCodes){
			_keyCodes = keyCodes;
			_modifierKeyCodes = modifierKeyCodes;
		}

        public SendKeysCommand(Action<IKeyboardSimulator> action){
            _action = action;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
			var inputSimulator = new InputSimulator.InputSimulator();
            if (_action != null){
				_action(inputSimulator.Keyboard);
                
            }
            else{
                if (_modifierKeyCodes.Any()){
                    inputSimulator.Keyboard.ModifiedKeyStroke(_modifierKeyCodes, _keyCodes);
                }
                else{
                    inputSimulator.Keyboard.KeyPress(_keyCodes);	
                }
            }
			
			adapter.Execute(new WaitCommand(100));
		}
	}
}