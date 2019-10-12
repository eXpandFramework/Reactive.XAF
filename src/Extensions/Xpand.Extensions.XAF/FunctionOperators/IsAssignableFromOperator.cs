using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.SystemModule;

namespace Xpand.Extensions.XAF.FunctionOperators{
    public class IsAssignableFromOperator : ICustomFunctionOperator{
        public const string OperatorName = "IsAssignableFrom";
        private static readonly IsAssignableFromOperator Instance = new IsAssignableFromOperator();

        static IsAssignableFromOperator(){
            CustomFunctionOperatorHelper.Register(Instance);
        }

        public Type ResultType(params Type[] operands){
            return typeof(bool);
        }

        public object Evaluate(params object[] operands){
            var type = Type.GetType(operands[1].ToString());
            return type != null && type.IsAssignableFrom((Type) operands[0]);
        }

        public string Name{ get; } = OperatorName;
    }
}