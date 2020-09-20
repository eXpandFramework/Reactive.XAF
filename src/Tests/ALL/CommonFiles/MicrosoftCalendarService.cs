using ALL.Tests;
using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;
using Task = System.Threading.Tasks.Task;

namespace ALL.Win.Tests{
    public static class MicrosoftCalendarService{
        public static async Task TestMicrosoftCalendarService(this ICommandAdapter commandAdapter){
            await commandAdapter.TestOfficeCloudService("Microsoft");
            await commandAdapter.TestOfficeCloudService("Cloud.Microsoft Event", nameof(Event.Subject));
        }
    }
}