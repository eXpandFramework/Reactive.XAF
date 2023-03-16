![](http://45-126-125-189.cloud-xip.com/nuget/v/Xpand.XAF.Modules.MasterDetail.svg?&style=flat) ![](http://45-126-125-189.cloud-xip.com/nuget/dt/Xpand.XAF.Modules.MasterDetail.svg?&style=flat)

[![GitHub issues](http://45-126-125-189.cloud-xip.com/github/issues/eXpandFramework/expand/MasterDetail.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AMasterDetail) [![GitHub close issues](http://45-126-125-189.cloud-xip.com/github/issues-closed/eXpandFramework/eXpand/MasterDetail.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AMasterDetail)
# About 

The `MasterDetail` module can help you create platform agnostic master detail `XAF` views using only the Model Editor. 

## Details
This is a `platform agnostic` module that satisfies the following conditions:
1. If a `DashboardView` contains a `one ListView and one DetailView` of the `same type`, then it will be Master-Detail `enabled by default`. It can be disabled by setting the `IModelDashboardViewMasterDetail.MasterDetail` to false.

   ![image](https://user-images.githubusercontent.com/159464/55990839-67af0180-5cb1-11e9-84cd-6ef0bb5d0137.png)

3. Each time a ListView selection change, it will synchronize the DetailView CurrentObject with the selected from the ListView.
2. `ALL CRUD` operations are `supported`. A valuable module for forcing the DetailView to open in edit mode is the [ViewEditModeModule](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Agnostic/ViewEditMode). Additionaly you can use the [AutoCommitModule](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Agnostic/AutoCommit), for auto commiting the DetailView.
3. Conditional detailviews can be configured from the model by creating `IModelMasterDetailViewObjectTypeLinks`

   ![image](https://user-images.githubusercontent.com/159464/55991766-b1005080-5cb3-11e9-9dc2-bee3dfb627ac.png)
### Examples
The module is integrated with the `ExcelImporter`, `XtraDashboard` modules.


Next screenshot is an example from ExcelImporter from the view tha maps the Excel columns with the BO members. 
<twitter>
![image](https://user-images.githubusercontent.com/159464/55381194-238e6500-552b-11e9-8314-f1b1132d09f3.png)
</twitter>

--- 

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.MasterDetail`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.MasterDetailModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net6.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.222.5
 |Xpand.Extensions|4.222.5
 |Xpand.Extensions.Reactive|4.222.5
 |Xpand.Extensions.XAF|4.222.5
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.222.5
 |Xpand.Patcher|3.0.17
 |System.Reactive|5.0.0
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Text.Json|7.0.2
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.222.5

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.MasterDetail.MasterDetailModule))
```


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.MasterDetail.MasterDetail). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

