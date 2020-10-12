using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Google.Apis.Calendar.v3.Data;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Office.Cloud.Google;
using Xpand.XAF.Modules.Office.Cloud.Google.Calendar;
using Xpand.XAF.Modules.Reactive.Services;
using CalendarService = Google.Apis.Calendar.v3.CalendarService;

namespace ALL.Tests{
    public static class GoogleCalendarService{
        public static IObservable<Unit> ConnectGoogleCalendarService(this ApplicationModulesManager manager)
            => manager.ConnectCloudCalendarService(() => (
                    Xpand.XAF.Modules.Office.Cloud.Google.Calendar.CalendarService.Updated, DeleteAllEvents(),
                    manager.InitializeCloudCalendarModule(office => office.Google().Calendar(),"Google")))
                .Merge(manager.WhenApplication(application => application.ExecuteCalendarCloudOperation()));

        private static IObservable<IObservable<Unit>> DeleteAllEvents() 
            => Xpand.XAF.Modules.Office.Cloud.Google.Calendar.CalendarService.Credentials.FirstAsync()
                .Select(client => {
                    var calendarService = client.credential.NewService<CalendarService>();
                    return calendarService.GetCalendar(returnDefault:true)
                        .SelectMany(list => calendarService.DeleteAllEvents(list.Id)).ToUnit();
                });

        private static IObservable<Unit> ExecuteCalendarCloudOperation(this XafApplication application) 
            => application.ExecuteCalendarCloudOperation(typeof(Event),
                () => application.AuthorizeGoogle(),
                authorize => authorize.SelectMany(credential => {
                    var calendarService = credential.NewService<CalendarService>();
                    return calendarService.GetCalendar(returnDefault:true)
                        .SelectMany(entry => calendarService.Events.QuickAdd(entry.Id, "Google-Cloud").ExecuteAsync());
                }),
                (authorize, o) => authorize.SelectMany(credential => {
                    var calendarService = credential.NewService<CalendarService>();
                    return calendarService.GetCalendar(returnDefault:true)
                        .SelectMany(entry => calendarService.Events
                            .Update(new Event(){Summary = "Google-Cloud-Updated",
                                        End = new EventDateTime(){DateTime = DateTime.Now.AddMinutes(1)},
                                        Start = new EventDateTime(){DateTime = DateTime.Now}
                                    }, entry.Id, o.CloudId).ExecuteAsync());
                }),
                (authorize, o) => authorize.SelectMany(credential => {
                    var calendarService = credential.NewService<CalendarService>();
                    return calendarService.GetCalendar(returnDefault:true)
                        .SelectMany(entry => calendarService.Events.Delete(entry.Id, o.CloudId).ExecuteAsync()).ToUnit();
                }), "Google");

    }
}