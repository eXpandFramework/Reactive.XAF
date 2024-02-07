![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.Blazor.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.Blazor.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/Blazor.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ABlazor) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Blazor.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ABlazor)
# About 

The `Blazor` package hosts and activates Blazor specific XAF artifacts (Editors, Services, etc).

## Details

---

**Credits:** to companies that [sponsor](https://github.com/sponsors/apobekiaris) parts of this package.

---

This `Blazor only` module currently contains: 

1. The `UploadFilePropertyEditor` and the `DisplayTextPropertyEditor` which are demoed in the next screencast.

<twitter tags="#Blazor">

[![BlazorFileUpload](https://user-images.githubusercontent.com/159464/102690443-2274fe00-420e-11eb-88e9-0d5014a7280c.gif)
](https://youtu.be/SroXOxf_m74)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/SroXOxf_m74)

---

2. The `ComponentPropertyEditor` which can be used to render any Blazor component/html lighting fast. 

<twitter tags="#WinForms #WebForms">

[![ComponentPropertyEditor](https://user-images.githubusercontent.com/131656/109025740-aee8e480-76c7-11eb-8b05-5dc4675fb924.gif)
](2)

</twitter>


[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/VyP53DkIgTc)


3. Additional Attributes to help you geneate powerful UI without much coding are the [ImgPropertyAttribute](https://github.com/eXpandFramework/Reactive.XAF/blob/master/src/Extensions/Xpand.Extensions.XAF/Attributes/ImgPropertyAttribute.cs), [ReadOnlyObjectViewAttribute](https://github.com/eXpandFramework/Reactive.XAF/blob/master/src/Extensions/Xpand.Extensions.XAF/Attributes/ReadOnlyObjectViewAttribute.cs), [UrlPropertyAttribute](https://github.com/eXpandFramework/Reactive.XAF/blob/master/src/Extensions/Xpand.Extensions.XAF/Attributes/UrlPropertyAttribute.cs) and all attributes that live in the [Xpand.Extensions.XAF.Attributes/](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Extensions/Xpand.Extensions.XAF/Attributes) namespace.

See them applied to a listview `ImgPropertyAttribute`, `UrlPropertyAttribute`

![image](https://user-images.githubusercontent.com/159464/184247680-7217af87-c637-45b5-ad59-b787b11dca6a.png)

`ReadOnlyObjectViewAttribute`

![image](https://user-images.githubusercontent.com/159464/184247883-db757729-e6e9-4e40-8090-af692a87a9dd.png)

Screenshot taken from the [Reactive.Rest](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Reactive.Rest) package.

4. Model configuration for the DxDataGrid

![image](https://user-images.githubusercontent.com/159464/184248412-0f2bac55-ef0f-49da-a92e-aa15f5b4b483.png)


**Possible future improvements:**

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Blazor`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.BlazorModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net6.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.Blazor**|**Any**
 |**DevExpress.ExpressApp.Validation.Blazor**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.232.3
 |Xpand.Extensions.Blazor|4.232.3
 |Xpand.Extensions.Reactive|4.232.3
 |Xpand.Extensions.XAF|4.232.3
 |Xpand.Extensions|4.232.3
 |Xpand.Extensions.XAF.Xpo|4.232.3
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.232.3
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Reactive|6.0.0
 |System.Interactive|5.0.0
 |Microsoft.Bcl.AsyncInterfaces|7.0.0
 |Microsoft.CodeAnalysis|4.2.0
 |System.Data.SqlClient|4.8.6
 |System.Text.Json|7.0.2
 |System.Threading.Tasks.Dataflow|7.0.0
 |System.Security.Cryptography.ProtectedData|8.0.0
 |System.CodeDom|6.0.0
 |System.Configuration.ConfigurationManager|6.0.1
 |System.ServiceModel.NetTcp|4.10.2
 |System.ServiceModel.Http|4.10.2
 |System.ServiceModel.Security|4.10.2
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.232.3

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Blazor.BlazorModule))
```



### Tests

The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Blazor)

