using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;

namespace ALL.Win.Tests{
    public static class MicrosoftTodoService{
        public static void TestMicrosoftTodoService(this ICommandAdapter commandAdapter) => 
            commandAdapter.TestOfficeCloudService("Default.Task",nameof(Task.Subject), nameof(Task.Description));
    }
}