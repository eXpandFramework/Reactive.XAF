using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.ExpressionExtensions;

namespace Xpand.Extensions.XAF.DetailViewExtensions{
    public static partial class DetailViewExtensions{
	    public static TEditor GetPropertyEditor<TEditor, TObject>(this DetailView detailView, Expression<Func<TObject, object>> memberName) 
		    where TEditor : class => detailView.GetPropertyEditor(memberName.MemberExpressionName()) as TEditor;

	    public static PropertyEditor GetPropertyEditor<TObject>(this DetailView detailView, Expression<Func<TObject, object>> memberName) 
		    => detailView.GetPropertyEditor(memberName.MemberExpressionName()) ;

	    public static PropertyEditor GetPropertyEditor(this DetailView detailView, string memberName) => detailView
            .GetItems<PropertyEditor>().FirstOrDefault(editor => editor.MemberInfo.Name ==memberName);
    }
}