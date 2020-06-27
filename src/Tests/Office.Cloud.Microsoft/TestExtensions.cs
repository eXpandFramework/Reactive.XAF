using System;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using DevExpress.ExpressApp;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Tests{
	public static class TestExtensions{
		public static void NewMicrosoftAuthentication(this IObjectSpaceProvider objectSpaceProvider){
			var type = typeof(TestExtensions);
			using (var manifestResourceStream = type.Assembly.GetManifestResourceStream(type, "AuthenticationData.json")){
				var token = Encoding.UTF8.GetBytes(new StreamReader(manifestResourceStream ?? throw new InvalidOperationException()).ReadToEnd());
				using (var objectSpace = objectSpaceProvider.CreateObjectSpace()){
					var authenticationOid = (Guid)objectSpace.GetKeyValue(SecuritySystem.CurrentUser);
					if (objectSpace.GetObjectByKey<MSAuthentication>(authenticationOid)==null){
						var authentication = objectSpace.CreateObject<MSAuthentication>();
                    
						authentication.Oid=authenticationOid;
						authentication.Token=token;
						objectSpace.CommitChanges();
					}
				}
			}
		}

		public static IObservable<GraphServiceClient> AuthorizeTestMS(this XafApplication application,bool aquireToken=true){
			if (aquireToken){
				application.ObjectSpaceProvider.NewMicrosoftAuthentication();
			}
            
			return aquireToken ? application.AuthorizeMS()
				: application.AuthorizeMS((exception, strings) => Observable.Throw<AuthenticationResult>(exception));
		}
	}
}