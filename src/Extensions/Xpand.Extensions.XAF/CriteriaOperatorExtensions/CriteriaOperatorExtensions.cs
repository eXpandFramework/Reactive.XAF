using System;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;

namespace Xpand.Extensions.XAF.CriteriaOperatorExtensions {
    public static partial class CriteriaOperatorExtensions {
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

        public static CriteriaOperator ToCriteria<T>(this Expression<Func<T, bool>> expression) 
            => CriteriaOperator.FromLambda(expression);
        
        public static CriteriaOperator ToCriteria(this string s) => CriteriaOperator.Parse(s);
    }
}