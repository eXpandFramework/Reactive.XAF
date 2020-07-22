using System.Linq;
using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class CheckListViewCommand:EasyTestCommand{
        private string[][] _rows;
        public const string Name = "CheckListView";

        public CheckListViewCommand(string tableName, int rowCount, params string[] columns):this(columns){
            Parameters.Add(new Parameter("RowCount",rowCount.ToString()));
            Parameters.MainParameter=new MainParameter(tableName);
        }

        public CheckListViewCommand(params string[] columns){
            if (columns.Any()){
                Parameters.Add(new Parameter($" Columns = {string.Join(",", columns)}"));
            }
            Parameters.MainParameter=new MainParameter("");
        }

        public void AddRows(params string[][] rows){
            _rows = rows;
        }


        protected override void ExecuteCore(ICommandAdapter adapter){
            if (_rows != null){
                Parameters.AddRange(_rows.Select((value, i) =>
                    new Parameter($"Row[{i}] = {string.Join(", ", value)}")));
                if (_rows.Length > 0 && Parameters["RowCount"] == null){
                    Parameters.Add(new Parameter("RowCount", _rows.Length.ToString()));
                }
            }

            adapter.Execute(this.ConnvertTo<CheckTableCommand>());
        }

    }

}