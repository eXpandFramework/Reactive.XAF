using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public class ActionNewCommand:EasyTestCommand{
        public const string Name = "ActionNew";


        protected override void ExecuteCore(ICommandAdapter adapter){
            var command = new DevExpress.EasyTest.Framework.Commands.ActionCommand{
                Parameters = {MainParameter = new MainParameter("New"), ExtraParameter = new MainParameter()}
            };
            command.Execute(adapter);
        }
    }
}