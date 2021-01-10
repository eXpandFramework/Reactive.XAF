using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.EasyTest.Framework;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.TestsLib.Common.EasyTest.Commands{
    public class FillObjectViewCommand<T> : FillObjectViewCommand{
        public FillObjectViewCommand(params (Expression<Func<T, object>> editor, string value)[] tuples) : base(
            tuples.Select(t => (t.editor.MemberExpressionCaption().CompoundName(), t.value)).ToArray()){
        }
    }

    public class FillObjectViewCommand:EasyTestCommand{
        private readonly IEnumerable<(string editor, string value)> _tuples;
        public const string Name = "FillObjectView";

        public FillObjectViewCommand(params (string editor,string value)[] tuples){
            _tuples = tuples;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            foreach (var command in _tuples.Select(_ => new FillEditorCommand(_.editor.CompoundName(),_.value){})){
                command.Execute(adapter);
            }

        }
    }
}