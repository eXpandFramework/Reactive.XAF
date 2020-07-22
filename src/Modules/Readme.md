# About

This namespace `Xpand.XAF.Modules` is used for projects that contain XAF modules. 


## Platform agnostic modules list

**Due to the large package number a substantial effort is needed even for simple tasks, like installation, package API discovery and version choosing. How to get the best out of them?**
</br><u>Traditionally:</u>
      You can `discover and install` the packages `one by one` looking for `incompatibilities` between them, by yourself, in each project you plan to consume them.
</br><u>Xpand.XAF.Modules Solution:</u>
    Use `only` the `three` container nuget packages [Xpand.XAF.Core.All](https://www.nuget.org/packages/Xpand.XAF.Core.All), [Xpand.XAF.Win.All](https://www.nuget.org/packages/Xpand.XAF.Win.All), [Xpand.XAF.Web.All](https://www.nuget.org/packages/Xpand.XAF.Web.All). They come with the next benefits:
    * Install only one package per platform with agnostic optional.
    * You will get a `copy-paste` module `registration` snippet. 
    * `All API` from all packages is available in the VS intellisense as soon as you start typing. 
    * You do `not` have to deal with versions `incompatibilities`.
    * No extra dependencies if package API is not used.
    * Only one entry in the Nuget Package Manager Console lists.
    * Only one entry in the Project/References list.

</br>In the next screencast we see how easy is to install all packages that target the Windows platform. It is recommended to use the Nuget `PackageReference` format. First we install all packages and make a note that a dependency is added for all, then we remove a few installation lines and we make a note how the assembly dependencies reflects only that used API. The assembly reference discovery was done with the help of the XpandPwsh [Get-AssemblyReference](https://github.com/eXpandFramework/XpandPwsh/wiki/Get-AssemblyReference) cmdlet.</br>

<twitter>

![Xpand XAF All](https://user-images.githubusercontent.com/159464/86915211-447c3780-c12a-11ea-973d-3096044dc22b.gif)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/LvxQ-U_0Sbg)



|PackageName|Version|[![Custom badge](https://xpandshields.azurewebsites.net/endpoint.svg?label=&url=https%3A%2F%2Fxpandnugetstats.azurewebsites.net%2Fapi%2Ftotals%2FXAF)](https://www.nuget.org/packages?q=Xpand.XAF)|Target|Platform
|---|---|---|---|---|
[Xpand.XAF.Core.All](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Core.All)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Core.All.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Core.All.svg?label=&style=flat)||Agnostic
[AutoCommit](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/AutoCommit)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.AutoCommit.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.AutoCommit.svg?label=&style=flat)|net461|Agnostic
[CloneMemberValue](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/CloneMemberValue)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.CloneMemberValue.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.CloneMemberValue.svg?label=&style=flat)|net461|Agnostic
[CloneModelView](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/CloneModelView)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.CloneModelView.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.CloneModelView.svg?label=&style=flat)|net461|Agnostic
[GridListEditor](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/GridListEditor)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.GridListEditor.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.GridListEditor.svg?label=&style=flat)|net461|Win
[HideToolBar](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/HideToolBar)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.HideToolBar.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.HideToolBar.svg?label=&style=flat)|net461|Agnostic
[LookupCascade](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/LookupCascade)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.LookupCascade.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.LookupCascade.svg?label=&style=flat)|net461|Web
[MasterDetail](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/MasterDetail)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.MasterDetail.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.MasterDetail.svg?label=&style=flat)|net461|Agnostic
[ModelMapper](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ModelMapper)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.ModelMapper.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.ModelMapper.svg?label=&style=flat)|net461|Agnostic
[ModelViewInheritance](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ModelViewInheritance)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.ModelViewInheritance.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.ModelViewInheritance.svg?label=&style=flat)|net461|Agnostic
[Office.Cloud.Microsoft](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Office.Cloud.Microsoft)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Office.Cloud.Microsoft.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Office.Cloud.Microsoft.svg?label=&style=flat)|net461|Agnostic
[Office.Cloud.Microsoft.Todo](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Office.Cloud.Microsoft.Todo)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.svg?label=&style=flat)|net461|Agnostic
[OneView](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/OneView)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.OneView.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.OneView.svg?label=&style=flat)|net461|Win
[PositionInListView](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/PositionInListView)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.PositionInListView.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.PositionInListView.svg?label=&style=flat)|net461|Agnostic
[ProgressBarViewItem](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ProgressBarViewItem)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.ProgressBarViewItem.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.ProgressBarViewItem.svg?label=&style=flat)|net461|Agnostic
[Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Reactive.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Reactive.svg?label=&style=flat)|net461|Agnostic
[Reactive.Logger](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Reactive.Logger.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Reactive.Logger.svg?label=&style=flat)|net461|Agnostic
[Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Reactive.Logger.Client.Win.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Reactive.Logger.Client.Win.svg?label=&style=flat)|net461|
[Reactive.Logger.Hub](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Hub)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Reactive.Logger.Hub.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Reactive.Logger.Hub.svg?label=&style=flat)|net461|Agnostic
[Reactive.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Win)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Reactive.Win.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Reactive.Win.svg?label=&style=flat)|net461|Win
[RefreshView](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/RefreshView)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.RefreshView.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.RefreshView.svg?label=&style=flat)|net461|Agnostic
[SequenceGenerator](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/SequenceGenerator)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.SequenceGenerator.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.SequenceGenerator.svg?label=&style=flat)|net461|Agnostic
[SuppressConfirmation](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/SuppressConfirmation)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.SuppressConfirmation.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.SuppressConfirmation.svg?label=&style=flat)|net461|Agnostic
[ViewEditMode](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ViewEditMode)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.ViewEditMode.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.ViewEditMode.svg?label=&style=flat)|net461|Agnostic
[ViewItemValue](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ViewItemValue)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.ViewItemValue.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.ViewItemValue.svg?label=&style=flat)|net461|Agnostic
[Xpand.XAF.Web.All](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Web.All)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Web.All.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Web.All.svg?label=&style=flat)||Web
[Xpand.XAF.Win.All](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Win.All)|![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Win.All.svg?label=&style=flat)|![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Win.All.svg?label=&style=flat)||Win




## Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new/choose)

![GitHub issues by-label](https://xpandshields.azurewebsites.net/github/issues/expandframework/expand/Standalone_XAF_Modules.svg) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Standalone_XAF_Modules.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AXAF+)

## Installation 
You have to install the packages from nuget `Install-Package ModuleName` and then register the module as per XAF docs see [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module). 


## Versioning
The modules are **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The modules follow the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).

### Efficient Package Management

Working with many nuget packages may be counter productive. So if you want to boost your productity make sure you go through the [Efficient Package Management](https://github.com/eXpandFramework/DevExpress.XAF/wiki/Efficient-package-management) wiki page.
