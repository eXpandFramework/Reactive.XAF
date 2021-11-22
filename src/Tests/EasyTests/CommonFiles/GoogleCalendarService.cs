using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;
using Task = System.Threading.Tasks.Task;

namespace ALL.Tests {
	public static class GoogleCalendarService {
		public static async Task TestGoogleCalendarService(this ICommandAdapter commandAdapter) {
			await commandAdapter.TestOfficeCloudService("Google");
			await commandAdapter.TestOfficeCloudService("Cloud.Google Event", nameof(Event.Subject));
		}
	}
}