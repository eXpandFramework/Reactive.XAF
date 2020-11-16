using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class ClearObjectsSelectionCommand:EasyTestCommand{
        public const string Name = "ClearObjectsSelection";
        protected override void ExecuteCore(ICommandAdapter adapter){
            this.ConvertTo<ClearSelectionCommand>().Execute(adapter);
        }
    }
}