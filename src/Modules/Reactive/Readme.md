![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.Reactive.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.Reactive.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/Reactive.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Reactive) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Reactive.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Reactive)
# About 

The `Reactive` module can be used to create XAF DSL implementations in a Reactive/Functional style. 

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: `

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|System.Reactive|4.1.5
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|1.0.33

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call when [XafApplication.SetupComplete](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.XafApplication.SetupComplete).
```ps1
((Xpand.XAF.Modules.ReactiveModule) Application.Modules.FindModule(typeof(Xpand.XAF.Modules.ReactiveModule))).Unload();
```

## Details


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Reactive)

### Examples
