using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Security;
using Xpand.Extensions.XAF.ActionExtensions;
using static DevExpress.ExpressApp.Security.SecurityOperations;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static bool CanRead(this XafApplication application, Type objectType)
            => application.Security.IsGranted(new PermissionRequest(objectType, Read));
        public static bool CanRead<T>(this ActionBase action, Expression<Func<T, bool>> expression){
            var objectSpace = action.View()?.ObjectSpace;
            return objectSpace?.CanRead(expression, action.Application) ?? action.Application.CanRead(expression);
        }

        public static bool CanRead<T>(this XafApplication application, Expression<Func<T,bool>> expression){
            using var objectSpace = application.CreateObjectSpace(typeof(T));
            return objectSpace.CanRead( expression,application);
        }

        private static bool CanRead<T>(this IObjectSpace objectSpace, Expression<Func<T, bool>> expression, XafApplication application){
            var target = objectSpace.GetObjectsQuery<T>().FirstOrDefault(expression);
            return target != null && application.CanRead(objectSpace, typeof(T), target);
        }


        public static bool CanRead(this XafApplication application,Type objectType,IObjectSpaceLink targetObject) 
            => application.CanRead(targetObject.ObjectSpace,objectType,targetObject);
        
        public static bool CanRead(this XafApplication application,IObjectSpace objectSpace,Type objectType,object targetObject) 
            => application.Security.IsGranted( new PermissionRequest(objectSpace, objectType,Read,targetObject));
    }
}