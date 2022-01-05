using DevExpress.EasyTest.Framework;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class NavigateCommand:EasyTestCommand{
        public const string Name = "Navigate";

        public NavigateCommand(string navigationItemCaption,bool compoundCaption=true) 
            => Parameters.MainParameter=new MainParameter(compoundCaption?navigationItemCaption.CompoundName():navigationItemCaption);

        protected override void ExecuteCore(ICommandAdapter adapter){
            var actionCommand = new DevExpress.EasyTest.Framework.Commands.ActionCommand();
            var parameterList = actionCommand.Parameters;
            parameterList.MainParameter=new MainParameter("Navigation");
            parameterList.ExtraParameter = Parameters.MainParameter;
            actionCommand.Execute(adapter);

        }
    }
}