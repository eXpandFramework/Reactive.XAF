using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.General;
using JetBrains.Annotations;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar{
    
    public interface IModelMicrosoftCalendar:IModelNode{
        IModelCalendar Calendar{ get; }
    }

    [PublicAPI]
    public interface IModelCalendar:IModelNode{
        [DefaultValue(CalendarService.DefaultTodoListId)]
        [Required]
        string DefaultCaledarName{ get; set; }
        [DataSourceProperty(nameof(NewCloudEvents))]
        [Required]
        IModelClass NewCloudEvent{ get; set; }
        [Browsable(false)]
        IModelList<IModelClass> NewCloudEvents{ get; }

        IModelCalendarItems Items{ get; }
    }

    
    [DomainLogic(typeof(IModelCalendar))]
    public static class ModelCalendarLogic{
        [PublicAPI]
        internal static IModelCalendar CalendarModel(this IModelApplication application)
            => application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Calendar();
        public static IObservable<IModelCalendar> CalendarModel(this IObservable<IModelMicrosoft> source) 
            => source.Select(modules => modules.Calendar());

        [UsedImplicitly]
        public static IModelClass Get_NewCloudEvent(this IModelCalendar modelCalendar)
            => modelCalendar.NewCloudEvents.FirstOrDefault(); 
        public static CalculatedModelNodeList<IModelClass> Get_NewCloudEvents(this IModelCalendar modelCalendar) 
            => modelCalendar.Application.BOModel.Where(c =>c.TypeInfo.IsPersistent&&!c.TypeInfo.IsAbstract&&typeof(IEvent).IsAssignableFrom(c.TypeInfo.Type) ).ToCalculatedModelNodeList();

        

        public static IModelCalendar Calendar(this IModelMicrosoft modelMicrosoft) 
            => ((IModelMicrosoftCalendar) modelMicrosoft).Calendar;

        [PublicAPI]
        public static IModelCalendar Calendar(this IModelOfficeMicrosoft reactiveModules) 
            => reactiveModules.Microsoft.Calendar();
    }

    public interface IModelCalendarItems : IModelList<IModelCalendarItem>,IModelNode{
    }

    public interface IModelCalendarItem:IModelSynchronizationType,IModelCallDirection,IModelObjectViewDependency{
    }

    public enum CallDirection{
        Both,
        In,
        Out
    }

}
