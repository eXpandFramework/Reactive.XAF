﻿using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.ViewExtensions{
	public static partial class ViewExtensions{
		public static XafApplication Application(this CompositeView view) => (XafApplication) view.GetPropertyValue("Application");
	}
}