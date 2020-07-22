![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Office.Cloud.Microsoft.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Office.Cloud.Microsoft.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Office.Cloud.Microsoft.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Office.Cloud.Microsoft) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Office.Cloud.Microsoft.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Office.Cloud.Microsoft)
# About 

The `Microsoft` authenticates against Azure Active Directory and queries the MSGraph endpoints.

## Details

---

**Credits:** to [Brokero](https://www.brokero.ch/de/startseite/) that [sponsor](https://github.com/sponsors/apobekiaris) the initial implementation of this module.

---

This is a `platform agnostic` module. 

#### App Service configuration

First off you have to create an Azure application following the next steps:

1. Go to [App registrations](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) in the Azure portal.
2. Select New registration, then enter an application name.
3. Select the Supported account types, depending on your case.
5. Select Register and copy the `Application (Client) ID` to the related XAF model entry.
7. From the left pane, select `Authentication`. 
   * If you target the XAF web click on `Add Platform`, select `Web` enter a uri like `http://localhost:2064/login.aspx` and and copy this value to the related XAF model entry. Enable the implicit grant flow by selecting both `Access Token` and `ID Tokens`.
   * If you target the XAF win click on `Add Platform` and select `Mobile and desktop applications`. Check on one of the predefined Url e.g the https://login.live.com/oauth20_desktop.srf and copy this value to the related XAF model entry.
6. From the left pane, select `Certificates & secrets` > New client secret. Enter a description, select the validity duration, and select Add. Copy the value into the related XAF model entry. This step is only required for Web applications.
8. From the left pane, select API permissions > Add a permission to configure additional endpoint access. In the [Query the MSGraph endpoints](https://github.com/eXpandFramework/DevExpress.XAF/tree/lab/src/Modules/Office.Cloud.Microsoft#query-the-msgraph-endpoints) you can see an example of how to use the API to query the User endpoint. Copy these permissions to into the related XAF model.
1. The related XAF model is available at:
   ![image](https://user-images.githubusercontent.com/159464/86536412-f1b73b80-beef-11ea-8cca-490aeb16bb7d.png)


#### Authentication

The module `does not replace nor requires` the XAF authentication. The module will use the credentials from the application configuration file authenticate/link it self with the Azure application. To authenticate, the end user must execute the `Sign in with Microsoft` action. If XAF has security installed the action is only active in current user profile, else it is always active. Once there is a valid authentication the Sign in with Microsoft action will be deactivated and the `Sing out Microsoft` will be activated.

For both platforms once the user authenticated the `RefreshToken` and `AccessToken` will be saved with the help of the `MSAuthentication` business object. When the AccessToken expires the module will use the RefreshToken to `silently` request a new AccessToken until the lifetime limit reached `(6 months)`. If the MSAuthentication contains data for the current user and a new AccessToken cannot be acquired, a message will notify the end user to navigate to his/her profile for authentication.

#### Query the MSGraph endpoints

In the screencast on the examples section, we executed the `Show MS Account Info` action to display a popup view with all the details of the connected MS account. Below is all the module code used for it:

```cs

internal static class ShowMSAccountInfoService{
	// The ShowMSAccountInfo action declaration. Refer to the Reactive module wiki for details
	public static SimpleAction ShowMSAccountInfo(this (AgnosticModule, Frame frame) tuple) => 
		tuple.frame.Action(nameof(ShowMSAccountInfo)).As<SimpleAction>();

	public static IObservable<Unit> ShowMSAccountInfo(this ApplicationModulesManager manager){
		//export the Microsoft.Graph.User as we want to display as XAF view for it
		manager.Modules.OfType<AgnosticModule>().First().AdditionalExportedTypes.Add(typeof(Microsoft.Graph.User));
		//The ShowMSAccountInfo registration. Refer to the Reactive module wiki for details.
		//also we publish the registration as we want to reuse it without running it twice
		var registerViewSimpleAction = manager.RegisterViewSimpleAction(nameof(ShowMSAccountInfo)).ActivateInUserDetails().Publish().RefCount(); 
		//when the application is available at runtime we chain the ShowMSAccountInfo action execute event to the ShowAccountInfoView method
		return manager.WhenApplication(application => registerViewSimpleAction.WhenExecute().ShowAccountInfoView().ToUnit())
			//subscribe early before an application is created to expose the action to the design time enviroment.
			.Merge(registerViewSimpleAction.ToUnit());
	}

	private static IObservable<User> ShowAccountInfoView(this IObservable<SimpleActionExecuteEventArgs> source) =>
		source.SelectMany(e => {
				e.ShowViewParameters.CreatedView = e.Action.Application.NewView(ViewType.DetailView, typeof(User));
				e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow;
				//we get the OutlookUser and display it on a view
				return e.Action.Application.OutlookUser().ObserveOn(SynchronizationContext.Current)
					.Do(user => e.ShowViewParameters.CreatedView.CurrentObject = user);
			});

	//authorize to get the MSClient and use it to query the Me endpoint
	private static IObservable<User> OutlookUser(this XafApplication application) =>
		application.AuthorizeMS().SelectMany(client => client.Me.Request().GetAsync());
}


```

#### Prerequisites

In order to execute the asynchronous operations:

1. The `Async` attribute the Default.aspx must be true.

   ```xml
   <%@ Page Language="C#" AutoEventWireup="true" Inherits="Default" EnableViewState="false"
    ValidateRequest="false" CodeBehind="Default.aspx.cs" Async="true" %>
   ```

2. The `AspNetSynchronizationContext` context should be used by setting the targetFramework to a value greater than 4.5.1 in the web.config.

   ```xml
   <system.web>
    <httpRuntime targetFramework="4.5.1"/>
   ```

**Possible future improvements:**

1. Authenticate against XAF.
1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples


Below is a demonstration of the package authenticating against `AAD` for both `Win/Web`. Also the API is used to call the `MSGraph` [Me](https://docs.microsoft.com/en-us/graph/api/user-get?view=graph-rest-1.0&tabs=http) endpoint for displaying the authenticated user info in a XAF view. At the bottom the [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) is reporting as the module is used. This demo it is Easytested [with this script](https://github.com/eXpandFramework/DevExpress.XAF/blob/master/src/Tests/ALL/CommonFiles/MicrosoftService.cs) for the last three XAF major versions, compliments of the `Xpand.VersionConverter`

<twitter>

[![Xpand XAF Modules Office Cloud Microsoft](https://user-images.githubusercontent.com/159464/86131887-e24e8180-baee-11ea-8c02-b64b2c639b6d.gif)](https://www.youtube.com/watch?v=XIczKjE2sFw)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://www.youtube.com/watch?v=XIczKjE2sFw)

Also, refer to the [Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Office.Cloud.Microsoft.Todo)


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Office.Cloud.Microsoft`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Office.Cloud.MicrosoftModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net461`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.Persistent.Base**|**Any**
|Fasterflect.Xpand|2.0.7
 |JetBrains.Annotations|2020.1.0
 |Microsoft.Graph.Beta|0.18.0-preview
 |Microsoft.Graph.Core|1.19.0
 |Microsoft.Identity.Client|4.13.0
 |Microsoft.IdentityModel.Protocols.OpenIdConnect|6.6.0
 |Microsoft.IdentityModel.Tokens|6.6.0
 |Microsoft.Owin|4.1.0
 |Microsoft.Owin.Host.SystemWeb|4.1.0
 |Microsoft.Owin.Security|4.1.0
 |Microsoft.Owin.Security.Cookies|4.1.0
 |Microsoft.Owin.Security.OpenIdConnect|4.1.0
 |Newtonsoft.Json|12.0.3
 |Owin|1.0.0
 |System.Reactive|4.4.1
 |Xpand.Extensions|2.202.38
 |Xpand.Extensions.Office.Cloud|2.202.38
 |Xpand.Extensions.Reactive|2.202.39
 |Xpand.Extensions.XAF|2.202.39
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.202.39
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.9

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Office.Cloud.Microsoft.Office.Office.Cloud.MicrosoftModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Office.Office.Cloud.Microsoft.Office.Office.Cloud.Microsoft). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

