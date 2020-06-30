using System;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.TestsLib;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Tests{
	[SetUpFixture]
	public class MicrosoftServiceTestConfiguration{
		[OneTimeSetUp]
		public void GlobalSetup(){
			var json = JsonConvert.DeserializeObject<dynamic>(
				File.ReadAllText($"{AppDomain.CurrentDomain.ApplicationPath()}\\AzureAppCredentials.json"));
			MicrosoftService.Configure((string) json.MSClientId, (string) json.RedirectUri); // Do login here.
		}

		[OneTimeTearDown]
		public void GlobalTeardown(){
		}
	}
}