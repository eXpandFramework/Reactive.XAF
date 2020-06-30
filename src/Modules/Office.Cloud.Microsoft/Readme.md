![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Office.Cloud.Microsoft.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Office.Cloud.Microsoft.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Office.Cloud.Microsoft.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Office.Cloud.Microsoft) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Office.Cloud.Microsoft.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Office.Cloud.Microsoft)

# About 

This package authenticates against Azure Active Directory and provides API for querying the MSGraph endpoints.

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
5. Select Register and copy the `Application (Client) ID` into the application configuration AppSettings `MSClientID` entry.
7. From the left pane, select `Authentication`. 
   * If you target the XAF web click on `Add Platform`, select `Web` enter a uri like `http://localhost:2064/login.aspx` and and copy this value to your Web.Config `RedirectUri` AppSettings entry. Enable the implicit grant flow by selecting both `Access Token` and `ID Tokens`.
   * If you target the XAF win click on `Add Platform` and select `Mobile and desktop applications`. Check on one of the predefined Url e.g the https://login.live.com/oauth20_desktop.srf and copy this value to your App.Config `RedirectUri` AppSettings entry.
6. From the left pane, select `Certificates & secrets` > New client secret. Enter a description, select the validity duration, and select Add. Copy the value into the application configuration AppSettings `MSClientSecret` entry. This step is only required for Web applications.
8. From the left pane, select API permissions > Add a permission to configure additional endpoint access. In the [Query the MSGraph endpoints](https://github.com/eXpandFramework/DevExpress.XAF/tree/lab/src/Modules/Office.Cloud.Microsoft#query-the-msgraph-endpoints) you can see an example of how to use the API to query the User endpoint.
1. Configure additional permissions and OAuth2 settings using the XAF Model editor.


#### Authentication

The module `does not replace nor requires` the XAF authentication. The module will use the credentials from the application configuration file authenticate/link it self with the Azure application. To authenticate, the end user must execute the `Sign in with Microsoft` action. If XAF has security installed the action is only active in current user profile, else it is always active. Once there is a valid authentication the Sign in with Microsoft action will be deactivated and the `Sing out Microsoft` will be activated.

For both platforms once the user authenticated the `RefreshToken` and `AccessToken` will be saved with the help of the `MSAuthentication` business object. When the AccessToken expires the module will use the RefreshToken to `silently` request a new AccessToken until the lifetime limit reached `(6 months)`. If the MSAuthentication contains data for the current user and a new AccessToken cannot be acquired, a message will notify the end user to navigate to his/her profile for authentication.

In the next screencast you can see the module in action for both Win and Web. At the bottom the [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) is reporting as the module is used.
<twitter>

![Xpand XAF Modules Office Cloud Microsoft](https://user-images.githubusercontent.com/159464/86131887-e24e8180-baee-11ea-8c02-b64b2c639b6d.gif)
</twitter>

#### Query the MSGraph endpoints

In the above screencast at the end we executed the `Show MS Account Info` action to display a popup view with all the details of the connected MS account. Below is all the module code used for it:

```cs

//contains all logic for the current context which is to Show the connect MSAccount info
internal static class ShowMSAccountInfoService{
	// The ShowMSAccountInfo action declaration. Refer to the Reactive module wiki for details
	public static SimpleAction ShowMSAccountInfo(this (SolutionTestModule, Frame frame) tuple) => 
		tuple.frame.Action(nameof(ShowMSAccountInfo)).As<SimpleAction>();

	public static IObservable<Unit> Connect(this ApplicationModulesManager manager, SolutionTestModule module){
		//export the Microsoft.Graph.User as we want to display as XAF view for it
		module.AdditionalExportedTypes.Add(typeof(Microsoft.Graph.User));
		//The ShowMSAccountInfo registration. Refer to the Reactive module wiki for details.
		//also we publish the registration as we want to reuse it without running it twice
		var registerViewSimpleAction = manager.RegisterViewSimpleAction(nameof(ShowMSAccountInfo)).Publish().RefCount(); 
		return manager.WhenApplication(application => application.ShowUserInfoView(registerViewSimpleAction)).ToUnit()
			.Merge(registerViewSimpleAction.ToUnit());
	}

	private static IObservable<Unit> ShowUserInfoView(this XafApplication application, IObservable<SimpleAction> registerViewSimpleAction) =>
		application.OutlookUser().CombineLatest(
			registerViewSimpleAction.WhenExecute(), //when the ShowMSAccountInfo execute is raized we combine it with the latest value of the connected user
			(user, e) => {
				//we show the view of the logged Microsoft.Graph.User
				e.ShowViewParameters.CreatedView =
					e.Action.Application.NewView(ViewType.DetailView, typeof(Microsoft.Graph.User));
				e.ShowViewParameters.CreatedView.CurrentObject = user;
				e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow;
				return Unit.Default;
			});

	private static IObservable<Microsoft.Graph.User> OutlookUser(this XafApplication application) =>
		//when the mainwindow created 
		application.WhenWindowCreated().When(TemplateContext.ApplicationWindow) 
			//skip if authentication needed
			.SelectMany(window => application.MicrosoftNeedsAuthentication().WhenDefault()) 
			//get the MSGraphClient
			.SelectMany(window => application.AuthorizeMS()) 
			//get the account info
			.SelectMany(client => client.Me.Request().GetAsync()); 
}

public sealed class SolutionTestModule : ModuleBase{
	//install the MicrosoftModule
	public SolutionTestModule() => RequiredModuleTypes.Add(typeof(MicrosoftModule)); 

	public override void Setup(ApplicationModulesManager moduleManager){
		base.Setup(moduleManager);
		//subscribe to the main observable of the ShowMSAccountInfoService
		moduleManager.Connect(this).Subscribe(this); 
	}
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

Refer to the [Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Office.Cloud.Microsoft.Todo)


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
|**DevExpress.ExpressApp**|**Any**
|Fasterflect.Xpand|2.0.7
 |JetBrains.Annotations|2020.1.0
 |System.ValueTuple|4.5.0
 |Xpand.Extensions|2.201.34.5
 |Xpand.Extensions.XAF|2.201.35.8
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.201.7

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Office.Cloud.Microsoft.Office.Office.Cloud.MicrosoftModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Office.Office.Cloud.Microsoft.Office.Office.Cloud.Microsoft). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

