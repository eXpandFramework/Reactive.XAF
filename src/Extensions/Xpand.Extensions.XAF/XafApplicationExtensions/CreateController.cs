using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
	public static partial class XafApplicationExtensions{
		static Controller CreateController(this ControllersManager controllersManager, Type controllerType,IModelApplication modelApplication){
			var registeredControllers = ((Dictionary<Type, Controller>) controllersManager.GetFieldValue("sharedControllersManager").GetFieldValue("registeredControllers"));
			return registeredControllers.TryGetValue(controllerType, out var sourceController)
				? sourceController.Clone(modelApplication,ServiceProvider(controllersManager)) : (Controller) controllerType.CreateInstance();
		}

		public static IServiceProvider ServiceProvider(this ControllersManager controllersManager) 
			=> (IServiceProvider)controllersManager.GetFieldValue("serviceProvider");

		public static DialogController CreateDialogController(this XafApplication application) => application.CreateController<DialogController>();

		public static Controller CreateController(this XafApplication application, Type controllerType){
			var controllersManager = application.ControllersManager();
			var result = controllersManager == null
				? (Controller)controllerType.CreateInstance()
				: controllersManager.CreateController(controllerType, application.Model);
			result.Application = application;
			return result;
		}
	}
}