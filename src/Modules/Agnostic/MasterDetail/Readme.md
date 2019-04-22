![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.MasterDetail.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.MasterDetail.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/MasterDetail.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AXAF+MasterDetail) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/MasterDetail.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AXAF+MasterDetail)
# About 

The `MasterDetail` module can help you create platform agnostic master detail `XAF` views using only the Model Editor. 
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
`.NetFramework: v4.6.1`

Name|Version
----|----
**DevExpress.Data**|**Any** **DevExpress.ExpressApp**|**Any**
Xpand.VersionConverter|1.0.11
 [Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Agnostic/Xpand.XAF.Modules.Reactive)|1.2.10
 fasterflect|2.1.3
 Ryder|0.8.0
 System.ValueTuple|4.5.0

## Issues
For [Bugs](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Bug%2C+Standalone_XAF_Modules,+MasterDetail&template=standalone-xaf-modules--bug-report.md&title=), [Questions](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Question%2C+Standalone_XAF_Modules,+MasterDetail&template=standalone-xaf-modules--question.md&title=) or [Suggestions](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Enhancement%2C+Standalone_XAF_Modules,+MasterDetail&template=standalone-xaf-modules--feature-request.md&title=) use main project issues.
## Details
The module satisfies the following conditions:
1. If a `DashboardView` contains a `one ListView and one DetailView` of the `same type`, then it will be Master-Detail `enabled by default`. It can be disabled by setting the `IModelDashboardViewMasterDetail.MasterDetail` to false.

   ![image](https://user-images.githubusercontent.com/159464/55990839-67af0180-5cb1-11e9-84cd-6ef0bb5d0137.png)

3. Each time a ListView selection change, it will synchronize the DetailView CurrentObject with the selected from the ListView.
2. `ALL CRUD` operations are `supported`. A valuable module for forcing the DetailView to open in edit mode is the [ViewEditModeModule](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Agnostic/ViewEditMode). Additionaly you can use the [AutoCommitModule](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Agnostic/AutoCommit, for auto commiting the DetailView.
3. Conditional detailviews can be configured from the model by creating `IModelMasterDetailViewObjectTypeLinks`

   ![image](https://user-images.githubusercontent.com/159464/55991766-b1005080-5cb3-11e9-9dc2-bee3dfb627ac.png)

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Modules/MasterDetail)

### Examples
The module is integrated with the `ExcelImporter`, `XtraDashboard` modules.


Next screenshot is an example from ExcelImporter from the view tha maps the Excel columns with the BO members. 

![image](https://user-images.githubusercontent.com/159464/55381194-238e6500-552b-11e9-8314-f1b1132d09f3.png)
