![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.ViewEditMode.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.ViewEditMode.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/ViewEditMode.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AXAF+ViewEditMode) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/ViewEditMode.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AXAF+ViewEditMode)
# About 

The `ViewEditMode` module controls the state of DetailView.ViewEditMode. Choose `Edit` mode to open a `DetailView` in edit mode. 

The module uses the next two strategies:
1. It monitors the `DetailView` creation and modifies its ViewEditMode property according to model configuration. However later ViewEditMode property modifications are allowed.
2. It monitors the `ViewEditMode` modifiation and cancels it if the `LockViewEditMode` attribute is used.
## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.ViewEditMode`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.ViewEditModeModule));
    ```

The module is not integrated with any `eXpandFramework` module. You have to install it as described.

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: v4.6.1`

Name|Version
----|----
**DevExpress.ExpressApp**|**Any**
Xpand.VersionConverter|1.0.6
 [Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Agnostic/Xpand.XAF.Modules.Reactive)|1.0.11
 fasterflect|2.1.3
 System.Reactive|4.1.3
 System.Runtime.CompilerServices.Unsafe|4.5.2
 System.Threading.Tasks.Extensions|4.5.2
 System.ValueTuple|4.5.0

## Issues
For [Bugs](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Bug%2C+XAF,+ViewEditMode&template=xaf--bug-report.md&title=), [Questions](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Question%2C+XAF,+ViewEditMode&template=xaf--question.md&title=) or [Suggestions](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Enhancement%2C+XAF,+ViewEditMode&template=xaf--feature-request.md&title=) use main project issues.
## Details
The module extends the `IModelDetailView` interface with the `IModelDetailViewViewEditMode`. 

![image](https://user-images.githubusercontent.com/159464/55380067-b7f6c880-5527-11e9-96a1-053fd44095e7.png)

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Modules/ViewEditMode)

### Examples

The module is valuable in scenarios similar to:
1. When you want to `navigate` from a `ListView` to a `DetailView` without the intermediate view which is set to View ViewEditMode.
2. When you develop a `master-detail` layout and you want to control the ViewEditMode state of your

`XtraDashboardModule` ,`ExcelImporterModule` are modules that use the `ViewEditModeModule`.  

Next screenshot is an example from ExcelImporter from the view tha maps the Excel columns with the BO members. 

![image](https://user-images.githubusercontent.com/159464/55381194-238e6500-552b-11e9-8314-f1b1132d09f3.png)
