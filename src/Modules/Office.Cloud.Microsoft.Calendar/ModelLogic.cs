using System;
using System.ComponentModel;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar{
    
    public interface IModelMicrosoftCalendar:IModelNode{
        IModelCalendar Calendar{ get; }
    }

    [PublicAPI]
    public interface IModelCalendar:IModelNode{
        [DefaultValue(CalendarService.DefaultTodoListId)]
        [Required]
        string DefaultCaledarName{ get; set; }
    }

    [DomainLogic(typeof(IModelCalendar))]
    public static class ModelCalendarLogic{
        
        public static IObservable<IModelCalendar> CalendarModel(this IObservable<IModelMicrosoft> source){
            return source.Select(modules => modules.Calendar());
        }


        public static IModelObjectViewsDependencyList ObjectViews(this IModelCalendar modelCalendar){
            return ((IModelObjectViews) modelCalendar).ObjectViews;
        }

        public static IModelCalendar Calendar(this IModelMicrosoft modelMicrosoft){
            return ((IModelMicrosoftCalendar) modelMicrosoft).Calendar;
        }

        public static IModelCalendar Calendar(this IModelOfficeMicrosoft reactiveModules){
            return reactiveModules.Microsoft.Calendar();
        }

    }

}
