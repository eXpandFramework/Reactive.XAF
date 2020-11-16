using System.Linq;
using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class CheckDetailViewCommand:EasyTestCommand{
        public const string Name = "CheckDetailView";

        public CheckDetailViewCommand(params (string editor,string value)[] editors){
            Parameters.AddRange(editors.Select(_ => new Parameter($"{_.editor} = {_.value}")));
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            adapter.Execute(this.ConvertTo<CheckFieldValuesCommand>());
        }
    }
}