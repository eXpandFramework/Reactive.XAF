![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.Reactive.Logger.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.Reactive.Logger.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/Reactive.Logger.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Reactive.Logger) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Reactive.Logger.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Reactive.Logger)
# About 

The `Reactive.Logger` module monitors calls to the Reactive delegates (OnNext, OnSubscribe, OnDispose, OnCompleted, OnError) and persists the calls info in the application datastore. For more head to the details section.

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Reactive.Logger`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Reactive.LoggerModule));
    ```

The module is not integrated with any `eXpandFramework` module. You have to install it as described.

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: `

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
 |**DevExpress.Persistent.Base**|**Any**
 |**DevExpress.ExpressApp.ConditionalAppearance**|**Any**
 |**DevExpress.Xpo**|**Any**
|akarnokd.reactive_extensions|0.0.27-alpha
 |System.Reactive|4.1.6
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|1.2.47
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|1.0.34

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call when [XafApplication.SetupComplete](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.XafApplication.SetupComplete).
```ps1
((Xpand.XAF.Modules.Reactive.LoggerModule) Application.Modules.FindModule(typeof(Xpand.XAF.Modules.Reactive.LoggerModule))).Unload();
```

## Details
The module extends the `IModelReactiveModules` to provide a list TraceSources allowing to configure them further. All reactive modules will be in this list. 
![image](https://user-images.githubusercontent.com/159464/64830050-63c43a00-d5d7-11e9-919d-ac5df92646af.png)


![image](https://user-images.githubusercontent.com/159464/55380067-b7f6c880-5527-11e9-96a1-053fd44095e7.png)


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Reactive.Logger)

### Examples

The module  can be used with all packages that use the API from Xpand.XAF.Modules.Reactive. It will persist the calls to the datastore using the `TraceEvent` object. Below we analyze what the logger logs when used from the [Xpand.XAF.Modules.Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/lab/src/Modules/Reactive.Logger.Client.Win). 
![image](https://user-images.githubusercontent.com/159464/65377524-f2ab1380-dcb5-11e9-9861-c8c1d8381023.png)
Clarification: The client application is designed to received the remote logs in real-time of other XAF applications, it does it using the RX API because the [ReactiveLogger](https://github.com/eXpandFramework/DevExpress.XAF/tree/lab/src/Modules/Reactive.Logger), [ReactiveLoggerHub](https://github.com/eXpandFramework/DevExpress.XAF/tree/lab/src/Modules/Reactive.Logger.Hub), [OneView](https://github.com/eXpandFramework/DevExpress.XAF/tree/lab/src/Modules/OneView), [GridListEditor](https://github.com/eXpandFramework/DevExpress.XAF/tree/lab/src/Modules/GridListEditor) and [Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/lab/src/Modules/Reactive    ) modules are installed, so this means it is like just any other XAF application and can log itself the same way. Let us analyze what we see when it starts reading it from bottom to top (Please give a brief to the previous links to understand better the analysis):

1. We have `7` subscriptions/rows (`7th column`) in the `RXLoggerClientApp (2nd column)` from the `OneView, Reactive, GridListEditor `modules (`3rd column`).  On `4th column` we have the `location` of each subscription. 
So from bottom to top, we can see that the `OneViewModule ShowView and HideMainWindow `methods will execute, apparently doing what their names implies but we do not know when this will happen, at the moment its only a subscription. We also see the same with the `GridListEditor and its RememberTopRow `method. Finally there is the `ReactiveModule` that subscribed to emit `WhenWindowCreated 2 times and 1 WhenViewOneFrame`. 
2. Now it's emit time `(OnNext) 6 times (7th column)` where we can understand that the the application `IsLoggedOn`. The `WhenWindowCreated called 3 times `which makes partially sense as we only had 2 subscription from step 1, probably the other one was before our analysis start. Next we see that an `ObjectSpace created` leading to `CompatibilityChecked`. Here we have again multiple emition from these method which makes it unclear what is happening but we can also guess depending on past and future calls. Either way we know that we are just after logon when at least one ObjectSpace was just created therefore the database exists.
3. From step 2 we know we after logon therefore the user model is also merged giving the signal to the `ReactiveLoggerHubModule` which gets the 30 listening ports 61456, 61486 (last column) with the 
4. Trash these rows as they do not tell us more than that an ObjectSpaceCreated or using the powerful DevExpress Grid,  filter out the Reactive modules calls as it tends to get chatty.
5. The rest of the rows follow the same pattern with the GridListEditor and OneView modules on the lead.

