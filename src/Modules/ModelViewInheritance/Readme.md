![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.ModelViewInheritance.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.ModelViewInheritance.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/ModelViewInheritance.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+ModelViewInheritance) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/ModelViewInheritance.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+ModelViewInheritance)
# About 

The `ModuleViewInheritance` module replaces the generator layer of a view by composing multiple unrelated view model differences.
## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.ModelViewInheritance`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.ModelViewInheritanceModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net461`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|Fasterflect.Xpand|2.0.6
 |System.ValueTuple|4.5.0
 |Xpand.Extensions|0.0.1
 |Xpand.Extensions.XAF|0.0.1
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|1.0.35

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the contructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.ModelViewInheritance.ModelViewInheritanceModule))
```

## Details
The module extends the model views nodes with the `IModelObjectViewMergedDifferences` interface to allow model view differences composition. 

![image](https://user-images.githubusercontent.com/159464/50849204-f80e3b00-137e-11e9-8c6c-0a93edffb954.png)

**v1.1.39**
[#471](https://github.com/eXpandFramework/eXpand/issues/471). Adds the `DeepMerge` attribute which if enable the module will recourse and merge the source view differences. 

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.ModelViewInheritance.ModelViewInheritance). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)
### Examples
Bellow are a few examples of how we use the module in `eXpandFramework`. 


![image](https://user-images.githubusercontent.com/159464/50846982-1709ce80-1379-11e9-877a-6a2e277867a7.png)

to derive a version with `Remember Me` support as below:

![image](https://user-images.githubusercontent.com/159464/50847225-b75ff300-1379-11e9-998d-bcc22bc4bd00.png)

The next `WorldCreator`modified version of `PersistentMemberInfo`:

![image](https://user-images.githubusercontent.com/159464/50848737-af09b700-137d-11e9-94f0-578a0a922455.png)


is used to derive a version for the `PersistentCoreTypeMemberInfo` like:

![image](https://user-images.githubusercontent.com/159464/50848552-399de680-137d-11e9-84dc-a1d574100b48.png)

and in addition one for the `PersistentCollectionMemberInfo` 

![image](https://user-images.githubusercontent.com/159464/50848410-e7f55c00-137c-11e9-8f4a-c9511d95455b.png)

