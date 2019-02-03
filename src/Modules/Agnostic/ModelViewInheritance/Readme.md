![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.ModelViewInheritance.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.ModelViewInheritance.svg?&style=flat)
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
## Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new/choose)
## Details
The module extends the model views nodes with the `IModelObjectViewMergedDifferences` interface to allow model view differences composition. 

![image](https://user-images.githubusercontent.com/159464/50849204-f80e3b00-137e-11e9-8c6c-0a93edffb954.png)


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Specifications/Modules/ModelViewInheritance)

### Examples
The module is already integrated in `eXpandFramework` and is installed with all modules, so there is no need for explicit registration.

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