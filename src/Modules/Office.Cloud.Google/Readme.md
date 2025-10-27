![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.Office.Cloud.Google.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.Office.Cloud.Google.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/Office.Cloud.Google.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AOffice.Cloud.Google) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Office.Cloud.Google.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AOffice.Cloud.Google)
# About 

The `Google` package authenticates against the Google Cloud services.

## Details

---

**Credits:** to [Brokero](https://www.brokero.ch/de/startseite/) that [sponsor](https://github.com/sponsors/apobekiaris) the initial implementation of this module.

---

This is a `platform agnostic` module. 

#### App Service configuration

First off you have to create an Azure application following the next steps:

1. Go to [console.developers.google.com/](https://console.developers.google.com/).
2. Select Credentials/Create Credentials/OAth client ID
3. Select `Desktop app` or `Web application`.
   * If web then add the Authorized redirect URI e.g. http://localhost/default.aspx.
5. Select Create and copy the `Your Client ID`, `Your Client Secret` to the related XAF model entry.
7. From the left pane, select `Library` and enable for the API you want to consume.
6. Choose the [appropriate scopes](https://developers.google.com/identity/protocols/oauth2/scopes) and copy them into the related model attribute.
1. The related XAF model is available at:
   ![image](https://user-images.githubusercontent.com/159464/89741475-5723bc80-da9a-11ea-81d4-e8c115a3d8f6.png)


#### Authentication

The module `does not replace nor requires` the XAF authentication. The module will use the credentials from the application configuration file authenticate/link it self with the Azure application. To authenticate, the end user must execute the `Sign in with Google` action. If XAF has security installed the action is only active in current user profile, else it is always active. Once there is a valid authentication the Sign in with Google action will be deactivated and the `Sing out Google` will be activated.

For both platforms once the user authenticated the `RefreshToken` and `AccessToken` will be saved with the help of the `GoogleAuthentication` business object. When the AccessToken expires the module will use the RefreshToken to `silently` request a new AccessToken. The RefreshToken never expires. If the GoogleAuthentication contains data for the current user and a new AccessToken cannot be acquired, a message will notify the end user to navigate to his/her profile for authentication.

#### Query the Google People api

In the screencast on the examples section, we executed the `Show Google Account Info` action to display a popup view with all the details of the connected Google account. Below is all the module code used for it:

```cs

internal static class ShowGoogleAccountInfoService{
	//action declaration refer to the Reactive module wiki
	public static SimpleAction ShowGoogleAccountInfo(this (AgnosticModule, Frame frame) tuple) 
		=> tuple.frame.Action(nameof(ShowGoogleAccountInfo)).As<SimpleAction>();

	public static IObservable<Unit> ShowGoogleAccountInfo(this ApplicationModulesManager manager){
		//export the google EmailAddress so we can display a XAF view for it
		manager.Modules.OfType<AgnosticModule>().First().AdditionalExportedTypes.Add(typeof(EmailAddress));
		var registerViewSimpleAction = manager.RegisterViewSimpleAction(nameof(ShowGoogleAccountInfo)).ActivateInUserDetails().Publish().RefCount(); 
		return manager.WhenApplication(application 
				//when the action executed show the Info View
				=> registerViewSimpleAction.WhenExecute().ShowAccountInfoView().ToUnit())
			//register the action for the design time Model Editor
			.Merge(registerViewSimpleAction.ToUnit());
	}

	private static IObservable<Person> ShowAccountInfoView(this IObservable<SimpleActionExecuteEventArgs> source) 
		=> source.SelectMany(e => {
				e.ShowViewParameters.CreatedView = e.Action.Application.NewView(ViewType.DetailView, typeof(EmailAddress));
				e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow;
				return e.Action.Application.GoogleUser().ObserveOn(SynchronizationContext.Current)
					.Do(user => e.ShowViewParameters.CreatedView.CurrentObject = user.EmailAddresses.First());
			});

	private static IObservable<Person> GoogleUser(this XafApplication application) 
	//authorize and then create the PeopleService to retrieve the auth google Person
		=> application.AuthorizeGoogle().NewService<PeopleService>()
			.SelectMany(service => {
				var request = service.People.Get("people/me");
				request.RequestMaskIncludeField = "person.emailAddresses";
				return request.ExecuteAsync(); //
			});


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


In the screencast below we see authentication against `Google/People/Me` service. To demo auth token database persistence we logOff and the clean the browser cookies. At the bottom the [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) is reporting as the module is used. This demo is Easytested [with this script](https://github.com/eXpandFramework/DevExpress.XAF/blob/master/src/Tests/ALL/CommonFiles/GoogleService.cs) for the last three XAF major versions, compliments of the `Xpand.VersionConverter`


**Blazor**

<twitter tags="#Blazor">

[![Xpand XAF Modules Office Cloud Google](https://user-images.githubusercontent.com/159464/97125250-012af080-173c-11eb-8504-a8378bceb7df.gif)](https://youtu.be/DOBL70tzsVM)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/DOBL70tzsVM)

**WindowsForms/WindowsDesktop/WebForms**

<twitter tags="#WinForms #WebForms">

[![Xpand XAF Modules Office Cloud Google](https://user-images.githubusercontent.com/159464/89726928-e8f2e180-da28-11ea-96fc-135719a18f46.gif)](https://www.youtube.com/watch?v=-pZjbGUChp0)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://www.youtube.com/watch?v=-pZjbGUChp0)




## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Office.Cloud.Google`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Office.Cloud.GoogleModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.Google.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net9.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.Persistent.Base**|**Any**
|Xpand.Extensions|4.251.7
 |Xpand.Extensions.Office.Cloud|4.251.7
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.251.7
 |Xpand.Extensions.Reactive|4.251.7
 |Xpand.Extensions.XAF|4.251.7
 |Xpand.Extensions.XAF.Xpo|4.251.7
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Text.Json|9.0.8
 |System.Reactive|6.0.1
 |Google.Apis.Auth|1.55.0
 |System.Threading.Tasks.Dataflow|9.0.8
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.251.7
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.251.7

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Office.Cloud.Google.Office.Office.Cloud.GoogleModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Office.Office.Cloud.Google.Office.Office.Cloud.Google). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

