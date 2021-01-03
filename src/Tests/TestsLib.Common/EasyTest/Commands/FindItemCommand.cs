using DevExpress.EasyTest.Framework;
using Shouldly;
using TextCopy;
using Xpand.TestsLib.Common.EasyTest.Commands.Automation;
using Xpand.TestsLib.Common.Win32;

namespace Xpand.TestsLib.Common.EasyTest.Commands{
    public class FindItemCommand : EasyTestCommand{
        private readonly string _item;
        private readonly bool _sendEnter;
        private readonly bool _assertMatch;

        public FindItemCommand(string item,bool sendEnter=true,bool assertMatch=true){
            _item = item;
            _sendEnter = sendEnter;
            _assertMatch = assertMatch;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.F,Win32Constants.VirtualKeys.Control));
            adapter.Execute(new WaitCommand(1000));
            adapter.Execute(new SendTextCommand(_item));
            adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Escape));
            adapter.Execute(new WaitCommand(1000));
            if (_assertMatch){
                adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.C,Win32Constants.VirtualKeys.Control));
                adapter.Execute(new WaitCommand(1000));
                new Clipboard().GetText().ShouldBe(_item);
            }
            if (_sendEnter){
                adapter.Execute(new WaitCommand(1000));
                adapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Return));
                adapter.Execute(new WaitCommand(1000));
            }

            
        }
    }
}