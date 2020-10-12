#if !NETCOREAPP3_1
using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ALL.Tests;
using DevExpress.ExpressApp;
using Microsoft.Graph;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.MicrosoftCalendarService{
    public static class MicrosoftCalendarService{
        public static IObservable<Unit> ConnectMicrosoftCalendarService(this ApplicationModulesManager manager)
            => manager.ConnectCloudCalendarService(() => (
                    CalendarService.Updated, DeleteAllEvents(),
                    manager.InitializeCloudCalendarModule(office => office.Microsoft().Calendar(),"Microsoft")))
                .Merge(manager.WhenApplication(application => application.ExecuteCalendarCloudOperation()));


        private static IObservable<IObservable<Unit>> DeleteAllEvents() 
            => CalendarService.Client.FirstAsync().Select(t => t.client.Me.Calendar.DeleteAllEvents());

        private static IObservable<Unit> ExecuteCalendarCloudOperation(this XafApplication application) 
            => application.ExecuteCalendarCloudOperation(typeof(Event),
                () => application.AuthorizeMS(),
                authorize => authorize.SelectMany(client => client.Me.Events.Request()
                    .AddAsync(new Event() {
                        Subject = "Microsoft-Cloud",
                        Start = new DateTimeTimeZone() {
                            DateTime = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                            TimeZone = TimeZoneInfo.Local.Id
                        },
                        End = new DateTimeTimeZone() {
                            DateTime = DateTime.Now.AddMinutes(2).ToString(CultureInfo.InvariantCulture),
                            TimeZone = TimeZoneInfo.Local.Id
                        }
                    })),
                (authorize, o) => authorize.SelectMany(c => c.Me.Events[o.CloudId].Request()
                    .UpdateAsync(new Event(){Subject = "Microsoft-Cloud-Updated"})),
                (authorize, o) => authorize.SelectMany(c => c.Me.Events[o.CloudId].Request()
                    .DeleteAsync().ToObservable()), "Microsoft");
    }

}
#endif