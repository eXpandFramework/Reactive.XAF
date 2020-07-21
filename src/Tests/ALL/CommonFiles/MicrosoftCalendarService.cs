using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;

namespace ALL.Win.Tests{
    public static class MicrosoftCalendarService{
        public static void TestMicrosoftCalendarService(this ICommandAdapter commandAdapter) => 
            commandAdapter.TestOfficeCloudService("Default.Scheduler Event",nameof(Event.Subject), nameof(Event.Description));
    }
}