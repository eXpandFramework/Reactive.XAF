using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class CheckListViewSelectionCommand<TObject> : CheckListViewSelectionCommand{
        public CheckListViewSelectionCommand(Expression<Func<TObject, object>> tableNameSelectotr,
            params (string column, string value)[] columns) : base(
            tableNameSelectotr.MemberExpressionCaption().CompoundName(), columns){
        }
    }

    public class CheckListViewSelectionCommand:EasyTestCommand{
        public CheckListViewSelectionCommand(string tableName,  params (string column,string value)[] columns){
            Parameters.MainParameter=new MainParameter(tableName);
            Parameters.Add(new Parameter($"Columns = {columns.Select(t => t.column).Join(", ")}"));
            Parameters.AddRange(columns.Select(_ => new Parameter($"Row = {_.value}")));
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            adapter.Execute(this.ConnvertTo<CheckTableSelectionCommand>());
        }
    }
}