using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.Office.Cloud;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar{
    
    public interface IModelMicrosoftCalendar:IModelNode{
        IModelCalendar Calendar{ get; }
    }

    public static class ModelCalendarLogic{
        internal static IModelCalendar Calendar(this IModelApplication application)
            => application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Calendar();

        public static IObservable<IModelCalendar> Calendar(this IObservable<IModelMicrosoft> source) 
            => source.Select(modules => modules.Calendar());

        public static IModelCalendar Calendar(this IModelMicrosoft modelMicrosoft) 
            => ((IModelMicrosoftCalendar) modelMicrosoft).Calendar;

        [PublicAPI]
        public static IModelCalendar Calendar(this IModelOfficeMicrosoft reactiveModules) 
            => reactiveModules.Microsoft.Calendar();
    }

}
