using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;

namespace ALL.Tests{
    public static class GoogleTasksService{
        public static void TestGoogleTasksService(this ICommandAdapter commandAdapter) =>
            commandAdapter.TestOfficeCloudService("Cloud.Google Task", nameof(Task.Subject), nameof(Task.Description));

    }
}