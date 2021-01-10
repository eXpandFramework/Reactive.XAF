using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.TestsLib.Common.EasyTest.Commands{
    public class FillEditorCommand:EasyTestCommand{
        private readonly bool _inlineEditor;
        public const string Name = "FillEditor";

        public FillEditorCommand(string editor,string value,bool inlineEditor=false){
            _inlineEditor = inlineEditor;
            if (inlineEditor){
                Parameters.Add(new Parameter($"Columns = {editor.CompoundName()}"));
                Parameters.Add(new Parameter($"Values = {value}"));
            }
            else{
                Parameters.Add(new Parameter(editor.CompoundName(),value));
            }
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            var command = _inlineEditor?(Command) this.ConvertTo<FillRecordCommand>():this.ConvertTo<FillFieldCommand>();
            adapter.Execute(command);
        }
    }
}