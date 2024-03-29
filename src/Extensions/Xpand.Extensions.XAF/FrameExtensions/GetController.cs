﻿using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.FrameExtensions{
    public static partial class FrameExtensions{
        public static Controller GetController(this Frame frame, System.Type controllerType) 
	        => (Controller) frame.CallMethod(new[]{controllerType}, "GetController");
        
        public static Controller GetController(this Frame frame, string controllerName) 
	        => frame.Controllers.Cast<Controller>().FirstOrDefault(controller => controller.Name==controllerName);
        
        public static IEnumerable<T> GetControllers<T>(this Frame frame) where T:Controller
	        => frame.GetControllers(typeof(T)).Cast<T>();
    }
}