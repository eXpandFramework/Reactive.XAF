using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public enum GridListEditorInlineCommand{
        New,
        Cancel,
        Update
    }
    public class ActionGridListEditorCommand:EasyTestCommand{
        public ActionGridListEditorCommand(GridListEditorInlineCommand command){
            Parameters.MainParameter=new MainParameter();
            Parameters.Add(new Parameter($"Inline{command} = ''"));
        }

        public ActionGridListEditorCommand((string column, string value) rowToEdit,string memberCaption=null) {
            Parameters.MainParameter=new MainParameter();
            Parameters.Add(new Parameter($"{rowToEdit.column} = {rowToEdit.value}"));
            Parameters.Add(new Parameter("InlineEdit = ''"));
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            adapter.Execute(this.ConvertTo<ExecuteTableActionCommand>());
        }
    }
}