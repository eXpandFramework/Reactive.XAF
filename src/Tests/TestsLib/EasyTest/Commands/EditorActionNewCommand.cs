using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class EditorActionNewCommand:EasyTestCommand{
        private readonly string _editor;
        public const string Name = "EditorActionNew";

        public EditorActionNewCommand(string editor){
            _editor = editor;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            var executeEditorActionCommand = new ExecuteEditorActionCommand{
                Parameters = {MainParameter = new MainParameter(_editor), ExtraParameter = new MainParameter("New")}
            };
            executeEditorActionCommand.Execute(adapter);

        }
    }
}