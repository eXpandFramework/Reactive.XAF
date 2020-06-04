using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
	public partial class ModelExtensions{
		public static IEnumerable<IModelPropertyEditor> PropertyEditorItems(this IModelObjectView modelObjectView, Type propertyEditorType = null) => modelObjectView
			.MemberViewItems(propertyEditorType).OfType<IModelPropertyEditor>();
	}
}