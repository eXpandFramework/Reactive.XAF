![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.Windows.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.Windows.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/Windows.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AWindows) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Windows.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AWindows)
# About 

The `Application.Windows` module hosts Windows Env related features like HideOnExit, Prompt, Startup, MultiInstance, NotifyIcon.

## Details
This is a `WinForms module`. To configure the `Application.Windows` module look at the Application/ReactiveModules/Windows model node:

<twitter>

![image](https://user-images.githubusercontent.com/159464/126012699-86b5f15f-76b4-4489-9275-5b4e27ec9829.png)

</twitter>

--- 

**Attributes:**

1. **Windows Node:**\
  `Startup`: Starts the application when current user login to Windows OS. 
1. **Exit/OnDeactivation Node:**\
  `CloseWindow`, `MinimizeWindow`: Results in closing/minimizing the active window when focus is lost. \
  `ExitApplication`: Results in application exit when focus is lost. 
1. **Exit/OnEscape Node:**\
  `CloseWindow`, `MinimizeWindow`: Results in closing/minimizing the active window when Escape key. \
  `ExitApplication`: Results in application exit when Escape key. 
  `ExitAfterModelEdit`: Results in application exit when XAF Model is modified. 
1. **Exit/OnExit Node:**\
  `HideMainWindowWindow`, `MinimizeMainWindow`: Results in hiding/minimizing the Main window when user tries to exit the application. 
1. **Exit/Prompt Node:**\
  `Enable`: Displays cancelable confirmation before application exiting. \
  `Message`: The message to display. 
1. **Form Node:**
  `ControlBox`, `MinimizeBox`, `MaximizeBox`, `ShowInTaskbar`, `ShowInTaskbar`, `FormBorderStyle`: Control Form properties.\
  `PopupWindows`: Applies previous attributes to popup windows e.g OneView package architecture.
1. **MultiInstance Node:**\
  `Disabled`: If true all other model node attributes are respected.\
  `NotifyMessage`: if empty then when additional application instances will terminate silently.\
  `FocusRunning`: When additional application instances terminated the main app will be focused.
1. **NotifyIcon Node:**\
  `Enabled`: If true all other model node attributes are respected.\
  `ShowText`,`ExitText`,`LogoffText`,`HideText`: They provide text for the notifyIcon menu. If empty the menu item is not visible.\
  `ShowOnDblClick`: Displays application on NotifyIcon double click.

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples

This module is used from the [Reactive.Logger.Client](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) to display only one View the `TraceEvent_ListView`

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Application.Windows`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Application.WindowsModule));
    ```

The module is not integrated with any `eXpandFramework` module. You have to install it as described.

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net9.0-windows`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.Win**|**Any**
|Xpand.Extensions.Reactive|4.251.6
 |Xpand.Extensions.XAF|4.251.6
 |Xpand.Extensions|4.251.6
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.251.6
 |System.Reactive|6.0.1
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Text.Json|9.0.8
 |Enums.NET|4.0.0
 |Xpand.Patcher|9.0.0
 |Microsoft.Extensions.DependencyInjection.Abstractions|9.0.8
 |Microsoft.CodeAnalysis|4.12.0
 |Microsoft.Extensions.Options|9.0.8
 |Microsoft.Extensions.Configuration.Abstractions|9.0.8
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.251.6
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.251.6

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Windows.WindowsModule))
```


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.OneView.OneView). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

