using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.EasyTest.Framework;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.TestsLib.EasyTest.Commands{

    public class ProcessRecordCommand<TObject,TColumn> : ProcessRecordCommand{
        public ProcessRecordCommand(params (Expression<Func<TColumn, object>> editor, string value)[] editors) : base(
            typeof(TObject).Name, editors.Select(t => (t.editor.MemberExpressionCaption(), t.value)).ToArray()){
        }

        public ProcessRecordCommand(Expression<Func<TObject, object>> tableSelector, params (Expression<Func<TColumn, object>> editor, string value)[] editors) 
            : base(tableSelector.MemberExpressionCaption(), editors.Select(t => (t.editor.MemberExpressionCaption(), t.value)).ToArray()){
        }
    }

    public class ProcessRecordCommand<T> : ProcessRecordCommand{
        public ProcessRecordCommand(params (string editor, string value)[] editors) : base(typeof(T).Name, editors){
        }

        public ProcessRecordCommand(Expression<Func<T, object>> tableSelector,
            params (string editor, string value)[] editors) : base(tableSelector.MemberExpressionCaption(), editors){
        }
    }

    public class ProcessRecordCommand:EasyTestCommand{
        public ProcessRecordCommand(string tableName,params (string editor,string value)[] editors){
            Parameters.MainParameter = new MainParameter(tableName.CompoundName());
            Parameters.AddRange(editors.Select(_ => new Parameter($"{_.editor} = {_.value}")));
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            adapter.Execute(this.ConvertTo<DevExpress.EasyTest.Framework.Commands.ProcessRecordCommand>());
        }
    }
}