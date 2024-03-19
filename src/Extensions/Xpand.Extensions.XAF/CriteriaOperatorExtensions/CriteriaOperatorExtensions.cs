using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DevExpress.Data.Filtering;
using DevExpress.Data.Helpers;
using DevExpress.Data.Linq;
using DevExpress.Data.Linq.Helpers;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.CriteriaOperatorExtensions {
    public static partial class CriteriaOperatorExtensions {
        static readonly MethodInfo FromLambdaMethod=typeof(CriteriaOperator).GetMethods(BindingFlags.Public|BindingFlags.Static)
            .First(info => info.Name=="FromLambda"&&info.GetGenericArguments().Length==1);

        static readonly ConcurrentDictionary<Type, MethodInfo> FromLambdaCache = new();

        class StringProcessor : CriteriaProcessorBase {  
            protected override void Process(OperandValue theOperand) {  
                base.Process(theOperand);  
                if (theOperand.Value != null) {  
                    var typeInfo = XafTypesInfo.Instance.FindTypeInfo(theOperand.Value.GetType());  
                    if (typeInfo is{ DefaultMember:{ } }) {  
                        theOperand.Value = typeInfo.DefaultMember.GetValue(theOperand.Value).ToString();  
                        return;  
                    }  
                    theOperand.Value = theOperand.Value.ToString();  
                }  
            }  
        }
        
        public static string UserFriendlyString(this CriteriaOperator criteriaOperator) {
            new StringProcessor().Process(criteriaOperator);
            return criteriaOperator?.ToString();
        }
        
        public static CriteriaOperator Combine(this CriteriaOperator criteriaOperator,string criteria,GroupOperatorType type=GroupOperatorType.And){
            var @operator = CriteriaOperator.Parse(criteria);
            return !criteriaOperator.ReferenceEquals(null) ? new GroupOperator(type, @operator, criteriaOperator) : @operator;
        }

        public static CriteriaOperator ToCriteria(this LambdaExpression expression,Type objectType)
            =>(CriteriaOperator)FromLambdaCache.GetOrAdd(objectType, t => FromLambdaMethod!.MakeGenericMethod(t))
                .Invoke(null, new object[] { expression });
        public static CriteriaOperator ToCriteria<T>(this Expression<Func<T, bool>> expression) 
            => CriteriaOperator.FromLambda(expression);
        
        public static CriteriaOperator ToCriteria(this string s) => CriteriaOperator.Parse(s);
        
        public static T[] ToArray<T>(this IQueryable<T> source)
            => source.ApplyToArray(typeof(T)).Cast<T>().ToArray();
        
        public static IQueryable<T> Where<T>(this IQueryable<T> source, CriteriaOperator criteria) 
            => (IQueryable<T>)source.AppendWhere(new CriteriaToExpressionConverter(), criteria);
    }
}