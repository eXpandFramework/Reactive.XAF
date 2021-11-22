using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.TestsLib.Common;
using Parameter = Xpand.TestsLib.EasyTest.Commands.Parameter;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class CheckDetailViewCommand<T>:CheckDetailViewCommand {
        public CheckDetailViewCommand(params (Expression<Func<T, object>> editor, string value)[] editors) : base(
            editors.Select(t => (t.editor.MemberExpressionCaption().CompoundName(), t.value)).ToArray()) { }
    }

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