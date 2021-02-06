using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.TestsLib.Common.EasyTest.Commands{

    public class CheckListViewCommand<TObject> : CheckListViewCommand{
        public CheckListViewCommand(Expression<Func<TObject, object>> tableSelector, int rowCount,
            params string[] columns) : base(tableSelector.MemberExpressionCaption().CompoundName(), rowCount, columns) { }
        public CheckListViewCommand(int rowCount, params string[] columns) : base(typeof(TObject).Name.CompoundName(), rowCount, columns){
        }
    }

    public class CheckListViewCommand:EasyTestCommand{
        private string[][] _rows;
        private bool _doNotCount;
        public const string Name = "CheckListView";

        public CheckListViewCommand(string tableName, int rowCount, params string[] columns):this(columns){
            Parameters.Add(new Parameter("RowCount",rowCount.ToString()));
            Parameters.MainParameter=new MainParameter(tableName);
        }

        public CheckListViewCommand(params string[] columns){
            if (columns.Any()){
                Parameters.Add(new Parameter($" Columns = {string.Join(",", columns.Select(s => s.CompoundName()))}"));
            }
            Parameters.MainParameter=new MainParameter("");
        }

        public void AddRows(bool doNotCount,params string[][] rows) {
            _rows = rows;
            _doNotCount = doNotCount;
        }

        public void AddRows(params string[][] rows) => _rows = rows;


        protected override void ExecuteCore(ICommandAdapter adapter){
            if (_rows != null){
                Parameters.AddRange(_rows.Select((value, i) =>
                    new Parameter($"Row[{i}] = {string.Join(", ", value)}")));
                if (_rows.Length > 0 && Parameters["RowCount"] == null&&!_doNotCount){
                    Parameters.Add(new Parameter("RowCount", _rows.Length.ToString()));
                }
            }

            adapter.Execute(this.ConvertTo<CheckTableCommand>());
        }

    }

}