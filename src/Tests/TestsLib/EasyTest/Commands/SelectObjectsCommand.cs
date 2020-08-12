using System.Linq;
using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class SelectObjectsCommand : EasyTestCommand{
        private readonly Command _command;
        public const string Name = "SelectObjects";

        public SelectObjectsCommand(MainParameter mainParameter=null){
            Parameters.Add(new Parameter("SelectAll = True"));
            _command = this.ConnvertTo<ExecuteTableActionCommand>();
            if (mainParameter != null) _command.Parameters.MainParameter = mainParameter;
        }

        public SelectObjectsCommand(string column,params string[] rows){
            Parameters.Add(new Parameter($"Columns = {column}"));
            Parameters.AddRange(rows.Select(s => new Parameter($"Row = {s}")));
            _command = this.ConnvertTo<SelectRecordsCommand>();
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            adapter.Execute(_command);
        }
    }
}