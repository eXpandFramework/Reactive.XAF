using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.SystemModule;

namespace Xpand.Source.Extensions.FunctionOperators{
    internal class PropertyExistsOperator : ICustomFunctionOperator{
        public const string OperatorName = "PropertyExists";
        private static readonly PropertyExistsOperator Instance = new PropertyExistsOperator();

        static PropertyExistsOperator(){
            CustomFunctionOperatorHelper.Register(Instance);
        }

        public Type ResultType(params Type[] operands){
            return typeof(bool);
        }

        public object Evaluate(params object[] operands){
            return operands[0].GetType().GetProperty((string) operands[1])!=null;
        }

        public string Name{ get; } = OperatorName;
    }
}