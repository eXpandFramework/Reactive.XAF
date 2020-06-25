using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Model;
using Fasterflect;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
	public static partial class XafApplicationExtensions{
		static Controller CreateController(this ControllersManager controllersManager, Type controllerType,IModelApplication modelApplication){
			var registeredControllers = ((Dictionary<Type, Controller>) controllersManager.GetFieldValue("registeredControllers"));
			return registeredControllers.TryGetValue(controllerType, out var sourceController)
				? sourceController.Clone(modelApplication) : (Controller) controllerType.CreateInstance();
		}

		public static Controller CreateController(this XafApplication application, Type controllerType){
			Controller result;
			var controllersManager = application.ControllersManager();
			if (controllersManager == null){
				result = (Controller) controllerType.CreateInstance();
			}
			else{
				result = controllersManager.CreateController(controllerType, application.Model);
			}

			result.Application = application;
			return result;
		}
	}
}