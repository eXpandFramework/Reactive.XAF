![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.SuppressConfirmation.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.SuppressConfirmation.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/SuppressConfirmation.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+SuppressConfirmation) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/SuppressConfirmation.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+SuppressConfirmation)
# About 

The `SuppressConfirmation` suppresses ObjectViews modification confirmations.

## Details
This is a `platform agnostic` module satisfies the following conditions:
1. When any Window `ObjectView` `changed` with `SuppressConfirmation` enabled, a signal will be created out of the [Frame.ViewChanged](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Frame.ViewChanged) event. For each signal emit the [ModificationsHandlingMode](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.SystemModule.ModificationsController.ModificationsHandlingMode) is set to not used.
2. When web for each signal emit both the `DevExpress.ExpressApp.Web.SystemModule.ASPxGridListEditorConfirmUnsavedChangesController`, `DevExpress.ExpressApp.Web.SystemModule.WebConfirmUnsavedChangesDetailViewController` will be deactivated.
<twitter>
![image](https://user-images.githubusercontent.com/159464/56219085-d2c05580-606e-11e9-9a8e-80e0a37b8285.png)
</twitter>

--- 

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples
The module is integrated with the `ExcelImporter`.

Next screenshot is an example from ExcelImporter from the view tha maps the Excel columns with the BO members. The editable DetailView on the right needs the SuppressConfirmation set to true, to get a natural flow UI without notifications.

![image](https://user-images.githubusercontent.com/159464/55381194-238e6500-552b-11e9-8314-f1b1132d09f3.png)


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.SuppressConfirmation`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.SuppressConfirmationModule));
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
 |System.Reactive|4.4.1
 |Xpand.Extensions.Reactive|2.202.39
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.202.39
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.9

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.SuppressConfirmation.SuppressConfirmationModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.SuppressConfirmation.SuppressConfirmation). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

