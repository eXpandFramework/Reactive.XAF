![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.BulkObjectUpdate.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.BulkObjectUpdate.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/BulkObjectUpdate.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ABulkObjectUpdate) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/BulkObjectUpdate.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ABulkObjectUpdate)
# About 

The `BulkObjectUpdate` module updates your Businesses data in bulk guided from flexible model configurations. 

## Details
This is a `platform agnostic` module, that generates an `BulkObjectUpdate` action using predefined model configuration like the next one.

```xml
<Application>
 <ReactiveModules>
  <BulkObjectUpdate>
   <Rules>
    <BulkObjectUpdateRule Caption="Update" ListView="TestTask_ListView" DetailView="TaskBulkUpdate_DetailView" IsNewNode="True" />
   </Rules>
  </BulkObjectUpdate>
 </ReactiveModules>
 <Views>
  <DetailView Id="TaskBulkUpdate_DetailView">
   <Items>
    <!--     Remove all PropertyEditors except the Status -->
   </Items>
   <Layout>
    <!--     Style the Status in the view layout -->
   </Layout>
  </DetailView>
 </Views>
</Application>
```

With the above model we enable the `BulkObjectUpdate` action for the `TestTask_ListView`. The action shows the `TaskBulkUpdate_DetailView` for which we have remove all editor except the `Task.Status` as this the only property we want to update.

The `BulkObjectUpdate` currently executes in the UI thread synchronously.

![image](https://user-images.githubusercontent.com/159464/143494042-59311a57-1fa5-4ebd-8942-b02b10a8f7e6.png)




### Examples

In the next screencast we create the previous model configuration resulting in the `BulkObjectUpdate` action activation. Then we test the action by updating the Status for two `Task` objects, in both the `Blazor and the Windows` platforms.   

<twitter tags="#BulkObjectUpdate #Blazor">

[![Xpand XAF Modules BulkObjectUpdate](https://user-images.githubusercontent.com/159464/143494273-0076056a-ad58-4e4f-ac34-0017af5ca19a.gif)
](https://youtu.be/DHfFtmBD4lw)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/Xy3IzZM6HYY)

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
|**DevExpress.ExpressApp.Validation**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.222.1
 |Xpand.Extensions.Reactive|4.222.1
 |Xpand.Extensions|4.222.1
 |Xpand.Extensions.XAF|4.222.1
 |Xpand.Extensions.XAF.Xpo|4.222.1
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.222.1
 |Xpand.Patcher|3.0.17
 |System.Reactive|5.0.0
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |Newtonsoft.Json|13.0.1
 |Xpand.Collections|1.0.4
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.222.1

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.MasterDetail.MasterDetailModule))
```


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.MasterDetail.MasterDetail). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

