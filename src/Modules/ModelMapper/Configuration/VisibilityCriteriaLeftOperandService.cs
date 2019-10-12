using System;
using System.ComponentModel;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Model;
using EnumsNET;
using Xpand.Extensions.XAF.FunctionOperators;

namespace Xpand.XAF.Modules.ModelMapper.Configuration{
    public enum VisibilityCriteriaLeftOperand{
        [Description(IsAssignableFromOperator.OperatorName+ "({0}"+nameof(IModelListView.EditorType)+",?)")]
        IsAssignableFromModelListVideEditorType,
        [Description(PropertyExistsOperator.OperatorName+ "({0},?)")]
        PropertyExists,
        [Description(IsAssignableFromOperator.OperatorName+ "({0}"+nameof(IModelCommonMemberViewItem.PropertyEditorType)+",?)")]
        IsAssignableFromModelCommonMemberViewItemPropertyEditorType
    }

    public static class VisibilityCriteriaLeftOperandService{
        public static string GetVisibilityCriteria(this VisibilityCriteriaLeftOperand leftOperand,object rightOperand,string path=""){
            if (leftOperand == VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType){
                rightOperand = ((Type) rightOperand).AssemblyQualifiedName;
            }

            
            var criteria = string.Format(leftOperand.AsString(EnumFormat.Description),path);
            return CriteriaOperator.Parse(criteria, rightOperand).ToString();
        }

    }
}