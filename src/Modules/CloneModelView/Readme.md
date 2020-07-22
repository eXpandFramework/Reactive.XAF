![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.CloneModelView.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.CloneModelView.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/CloneModelView.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+CloneModelView) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/CloneModelView.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+CloneModelView)
# About

The `CloneModelView` package generated additional default design time model views.

## Details
This is a `platform agnostic` module. Using the `CloneModelViewAttribute` in your Bussiness Objects you can:

1. Create one or many `DetailViews` or `ListViews` or `LookupListViews`.
2. Additionally for the cloned view you can configure if it will be the default view for the Business Object.
3. If you cloned a `ListView` it is possible the configure related `DetailView`

<twitter>
Next snippet is taken from the ModelDifference module.

```cs
 [RuleCombinationOfPropertiesIsUnique("MDO_Unique_Name_Application", DefaultContexts.Save, nameof(Name)+"," +nameof(PersistentApplication)+","+nameof(DeviceCategory))]
    [CreatableItem(false), NavigationItem("Default"), HideFromNewMenu]
    [ModelDefault("Caption", Caption), ModelDefault("IsClonable", "True"), VisibleInReports(false)]
    [CloneView(CloneViewType.DetailView, "MDO_DetailView",true)]
    [CloneView(CloneViewType.ListView, "MDO_ListView_Tablet",true)]
    [CloneView(CloneViewType.ListView, "MDO_ListView_Desktop",true)]
    [CloneView(CloneViewType.ListView, "MDO_ListView_Mobile",true)]
    [CloneView(CloneViewType.ListView, "MDO_ListView_All",true)]
    [CloneView(CloneViewType.ListView, "MDO_ListView", true)]
    [Appearance("Disable DeviceCategory for win models", AppearanceItemType.ViewItem,
        "EndsWith([" + nameof(PersistentApplication) + "." + nameof(BaseObjects.PersistentApplication.ExecutableName) +"], '.exe')", 
        Enabled = false, TargetItems = nameof(DeviceCategory))]
    [RuleCombinationOfPropertiesIsUnique(nameof(PersistentApplication)+","+nameof(DifferenceType)+","+nameof(CombineOrder))]
    public class ModelDifferenceObject : XpandCustomObject, IXpoModelDifference {

```

</twitter>

--- 

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.CloneModelView`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.CloneModelViewModule));
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
 |Xpand.Extensions.XAF|2.202.39
 |Xpand.Patcher|2.0.23
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.9

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.CloneModelView.CloneModelViewModule))
```


### Tests

The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.CloneModelView.CloneModelView). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

