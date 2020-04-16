using System.Collections.Generic;
using System.Linq;
using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class FillObjectViewCommand:EasyTestCommand{
        private readonly IEnumerable<(string editor, string value)> _tuples;
        public const string Name = "FillObjectView";

        public FillObjectViewCommand(params (string editor,string value)[] tuples){
            _tuples = tuples;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            foreach (var command in _tuples.Select(_ => new FillEditorCommand(_.editor,_.value))){
                command.Execute(adapter);
            }

        }
    }
}