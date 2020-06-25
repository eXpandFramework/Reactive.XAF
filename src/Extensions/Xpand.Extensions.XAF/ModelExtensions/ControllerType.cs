using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Model;
using Fasterflect;

namespace Xpand.Extensions.XAF.ModelExtensions{
	public static partial class ModelExtensions{
		public static Type ControllerType(this IModelController controller, ControllersManager manager) =>
			((IEnumerable<Controller>) manager.GetPropertyValue("Controllers")).Where(_ => _.Name == controller.Name)
			.Select(_ => _.GetType()).FirstOrDefault();
	}
}