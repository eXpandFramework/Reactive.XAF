using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;

using Xpand.Extensions.Office.Cloud;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Calendar{
    
    public interface IModelGoogleCalendar:IModelNode{
        IModelCalendar Calendar{ get; }
    }

    public static class ModelCalendarLogic{
        internal static IModelCalendar Calendar(this IModelApplication application)
            => application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().Calendar();

        public static IObservable<IModelCalendar> Calendar(this IObservable<IModelGoogle> source) 
            => source.Select(modules => modules.Calendar());

        public static IModelCalendar Calendar(this IModelGoogle modelGoogle) 
            => ((IModelGoogleCalendar) modelGoogle).Calendar;

        
        public static IModelCalendar Calendar(this IModelOfficeGoogle reactiveModules) 
            => reactiveModules.Google.Calendar();
    }
    
}
