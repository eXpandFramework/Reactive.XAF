using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.Win32;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class FindItemCommand : EasyTestCommand{
        private readonly string _item;
        private readonly bool _sendEnter;

        public FindItemCommand(string item,bool sendEnter=false){
            _item = item;
            _sendEnter = sendEnter;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.F,Win32Constants.VirtualKeys.Control));
            adapter.Execute(new WaitCommand(1000));
            adapter.Execute(new SendTextCommand(_item));
            adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Escape));
            adapter.Execute(new WaitCommand(1000));
            if (_sendEnter){
                adapter.Execute(new WaitCommand(1000));
                adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Return));
                adapter.Execute(new WaitCommand(1000));
            }
        }
    }
}