using ALL.Tests;
using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;
using Task = System.Threading.Tasks.Task;

namespace ALL.Win.Tests{
#if !NETCOREAPP3_1
    public static class MicrosoftTodoService{
        public static async Task TestMicrosoftTodoService(this ICommandAdapter commandAdapter) 
            => await commandAdapter.TestOfficeCloudService("Cloud.Microsoft Task",nameof(DevExpress.Persistent.BaseImpl.Task.Subject));
    }
#endif
}