using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
	public partial class ModelExtensions{
		public static IEnumerable<IModelMemberViewItem> DomainComponentItems(this IModelObjectView modelObjectView, Type propertyEditorType = null) => modelObjectView != null
				? modelObjectView.MemberViewItems(propertyEditorType).Where(editor => editor.ModelMember.MemberInfo.MemberTypeInfo.IsDomainComponent)
				: Enumerable.Empty<IModelMemberViewItem>();
	}
}