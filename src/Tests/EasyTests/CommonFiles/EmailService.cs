using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Shouldly;
using Xpand.TestsLib.Common.BO;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using ActionCommand = Xpand.TestsLib.EasyTest.Commands.ActionCommands.ActionCommand;

namespace Web.Tests {
	public static class EmailService {
		public static async Task TestEmail(this ICommandAdapter adapter, string applicationPath)
			=> await TestEmailCreation(adapter, applicationPath, "ProductName0", () => {
				adapter.Execute(new NavigateCommand("Default.Product"));
				adapter.Execute(new ProcessRecordCommand<Product>((nameof(Product.ProductName), "ProductName0")));
				adapter.Execute(new ActionCommand(nameof(Xpand.XAF.Modules.Email.EmailService.Email)));
			});

		public static async Task TestEmailCreation(this ICommandAdapter adapter, string applicationPath,
			string productName, Action sendEmail) {
			var pickupDirectory = $"{Path.GetDirectoryName(applicationPath)}\\TestApplication";
			if (Directory.Exists(pickupDirectory)) {
				Directory.Delete(pickupDirectory, true);
			}

			sendEmail();
			await adapter.Execute(() => {
				Directory.Exists(pickupDirectory).ShouldBeTrue();
				var files = Directory.GetFiles(pickupDirectory, "*.eml");
				files.Length.ShouldBe(1);
				File.ReadAllText(files.First()).ShouldContain(productName);
				Directory.Delete(pickupDirectory, true);
			});
		}
	}
}