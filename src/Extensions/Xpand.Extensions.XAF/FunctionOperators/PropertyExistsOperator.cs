using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.SystemModule;

namespace Xpand.Extensions.XAF.FunctionOperators{
    public class PropertyExistsOperator : ICustomFunctionOperator{
        public const string OperatorName = "PropertyExists";
        private static readonly PropertyExistsOperator Instance = new PropertyExistsOperator();

        static PropertyExistsOperator() => CustomFunctionOperatorHelper.Register(Instance);

        public System.Type ResultType(params System.Type[] operands) => typeof(bool);

        public object Evaluate(params object[] operands) => operands[0].GetType().GetProperty((string) operands[1])!=null;

        public string Name{ get; } = OperatorName;
    }
}