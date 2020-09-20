using ALL.Tests;
using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;

namespace ALL.Win.Tests{
    public static class MicrosoftTodoService{
        public static async System.Threading.Tasks.Task TestMicrosoftTodoService(this ICommandAdapter commandAdapter) 
            => await commandAdapter.TestOfficeCloudService("Cloud.Microsoft Task",nameof(Task.Subject));
    }
}