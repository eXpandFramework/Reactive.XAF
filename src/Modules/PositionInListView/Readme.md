![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.PositionInListView.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.PositionInListView.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/PositionInListView.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+PositionInListView) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/PositionInListView.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+PositionInListView)
# About 

The `PositionInListView`, controls how objects are positioned in a ListView at runtime.    

## Details
---

**Credits:** to the Company (wants anonymity) that [sponsor](https://github.com/sponsors/apobekiaris) the initial implementation of this module. 

---
This is a `platform agnostic` module that is designed to sort a ListView based on a existing Business Object member. It is possible to define one configuration for each ListView and choose the sorting member from the model as shown:

![image](https://user-images.githubusercontent.com/159464/82748810-7ec58b80-9dad-11ea-8e00-6f98cc426f19.png)

For the configured ListViews the `MoveObjectUp` and `MoveObjectDown` actions will be active and they can be used to change the order. The module on the background swaps the values of the model configured `PositionMember` on each action execute resulting in a persistent ListView order.

The ListView sorting happens when a ListView is created on the CollectionSource object. For cases that the CollectionSource is not sortable e.g. (non-persistent) sorting is done explicitly. In addition ListView sorting and grouping from the UI Editor are disabled and the `PositionMember` modification are not committed explicitly.

For new Business objects the module will automatically update the configured members as per model configuration as shown:

![image](https://user-images.githubusercontent.com/159464/82749132-cbaa6180-9daf-11ea-87bd-0a2a91753636.png)

If a `NewObjectsStrategy` for the ModelClass is not configured it defaults to `Last`. The strategy is not applied to objects created from a ModuleUpdater.

In the screencast we create three customers at runtime and demo the feature by executing the MoveUp/MoveDown actions and close/reopen the view`. At the bottom the [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win)

<twitter>

[![sqFoseHS2q](https://user-images.githubusercontent.com/159464/82759129-e4d50180-9df3-11ea-8bb9-eb6b36452c51.gif)](https://www.youtube.com/watch?v=JBoVNXo19ek)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://www.youtube.com/watch?v=JBoVNXo19ek)


---

**Possible future improvements:**

1. Moving multiple positions at once.
2. Moving multiple objects.
4. Conditional NewObjectsStrategy.
5. Conditional persistance PositionMember modifications.
6. Enable temporarily UI sorting and grouping on the ListView.
3. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.PositionInListView`.

    The above only references the dependencies and next steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.PositionInListViewModule));
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
 |JetBrains.Annotations|2019.1.3
 |System.ValueTuple|4.5.0
 |Xpand.Extensions|2.201.29
 |Xpand.Extensions.XAF|2.201.29
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.201.7

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.PositionInListView.PositionInListViewModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.PositionInListView.PositionInListView). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

