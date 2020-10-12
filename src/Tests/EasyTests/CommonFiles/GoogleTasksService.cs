using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;

namespace ALL.Tests{
    public static class GoogleTasksService{
        public static async System.Threading.Tasks.Task TestGoogleTasksService(this ICommandAdapter commandAdapter) 
            => await commandAdapter.TestOfficeCloudService("Cloud.Google Task", nameof(Task.Subject));

    }
}