using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands{
    public class ActionEditObjectCommand:SelectObjectsCommand{
        public new const string Name = "EditObject";

        public ActionEditObjectCommand(string column, params string[] rows) : base(column, rows){
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            new ClearObjectsSelectionCommand().Execute(adapter);
            base.ExecuteCore(adapter);
            new DevExpress.EasyTest.Framework.Commands.ActionCommand(){
                Parameters = {MainParameter = new MainParameter("Edit"),ExtraParameter = new MainParameter()}
            }.Execute(adapter);
        }
    }
}