using System;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static Type CalculateEditorType(this IModelMember modelMember)
            => new MemberEditorInfoCalculator().GetEditorType(modelMember);
    }
}