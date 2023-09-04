using System.Linq;
using System.Linq.Expressions;

namespace Xpand.Extensions.ExpressionExtensions {
    public static partial class ExpressionExtensions {
        public static T FuseAny<T>(this T expression, params LambdaExpression[] expressions) where T : Expression
            => (T)new ExpressionReplacer(expressions).Visit(expression);
        class ExpressionReplacer : ExpressionVisitor{
            private readonly LambdaExpression[] _expressions;

            public ExpressionReplacer(LambdaExpression[] expressions) => _expressions = expressions;
        
            protected override Expression VisitMethodCall(MethodCallExpression node){
                if (node.Method.Name == "Any"){
                    var arg = node.Arguments[0];
                    if (arg.Type.IsGenericType){
                        var genericArguments = arg.Type.GetGenericArguments();
                        if (genericArguments.Length == 1){
                            var type = genericArguments.First();
                            var lambdaExpression = _expressions.FirstOrDefault(expression => expression.Parameters.First().Type==type);
                            if (lambdaExpression != null){
                                return typeof(Enumerable).Call("Any", new[]{ type }, arg, lambdaExpression);    
                            }
                        }
                    }
                }
                return base.VisitMethodCall(node);
            }
        }

    }
}