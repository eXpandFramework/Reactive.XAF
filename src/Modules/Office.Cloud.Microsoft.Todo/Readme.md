![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Office.Cloud.Microsoft.Todo.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Office.Cloud.Microsoft.Todo) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Office.Cloud.Microsoft.Todo.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Office.Cloud.Microsoft.Todo)
# About 

This package provides a two way synchronization between XAF Bushiness objects that implement the `ITask` interface and the `Office365 OutlookTask` entities.

## Details
This is a `platform agnostic` module. For each platform you need to create an Azure application following the public guides.

#### Authentication

The module does not replace XAF authentication and is a mandatory requirement. For both platform (Win,Web) an `AzureAppCredentials.json` must exit in the application directory and must look like:

```json
{
  "MSClientId": "The_AzureAppId",
  "MSAppSecret": "The_AzureApp_Secret", //applicable only to web 
  "RedirectUri": "http://localhost:2064/login.aspx"
}
```

The module will use this json file to allow the XAF end user to authenticate/link it self with the Azure application. For authentication the end user must execute the `AuthenticateMS` action located in his profile details.

For both platforms once the user authenticated the `RefreshToken` and `AccessToken` are saved with the help of the `MSAuthentication` business object. When the AccessToken expires the module will use the RefreshToken to **silently** request a new AccessToken until the lifetime limit reached (6 months). If the MSAuthentication contains data for the current user and a new AccessToken cannot be acquired, a message will notify the end user to navigate to his/her profile for authentication.

#### Model configuration

* The AuthenticateMS action styling respects the Microsoft branding guidelines and you can use as is. It is possible however to change the used images from the model.
* The synchronization of ITask <-> OutlookTask takes place each time a pre-configured model view is shown.


**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples
Bellow are a few examples of how we use the module in `eXpandFramework`. 

<twitter>
![image](https://user-images.githubusercontent.com/159464/50846982-1709ce80-1379-11e9-877a-6a2e277867a7.png)

to derive a version with `Remember Me` support as below:

![image](https://user-images.githubusercontent.com/159464/50847225-b75ff300-1379-11e9-998d-bcc22bc4bd00.png)
</twitter>
The next `WorldCreator`modified version of `PersistentMemberInfo`:

![image](https://user-images.githubusercontent.com/159464/50848737-af09b700-137d-11e9-94f0-578a0a922455.png)


is used to derive a version for the `PersistentCoreTypeMemberInfo` like:

![image](https://user-images.githubusercontent.com/159464/50848552-399de680-137d-11e9-84dc-a1d574100b48.png)

and in addition one for the `PersistentCollectionMemberInfo` 

![image](https://user-images.githubusercontent.com/159464/50848410-e7f55c00-137c-11e9-8f4a-c9511d95455b.png)


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Office.Cloud.Microsoft.TodoModule));
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
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.Office.Cloud.Microsoft.TodoModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Office.Cloud.Microsoft.Todo.Office.Cloud.Microsoft.Todo). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

