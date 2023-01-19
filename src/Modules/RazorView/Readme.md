![](http://45-126-125-189.cloud-xip.com/nuget/v/Xpand.XAF.Modules.RazorView.svg?&style=flat) ![](http://45-126-125-189.cloud-xip.com/nuget/dt/Xpand.XAF.Modules.RazorView.svg?&style=flat)

[![GitHub issues](http://45-126-125-189.cloud-xip.com/github/issues/eXpandFramework/expand/RazorView.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ARazorView) [![GitHub close issues](http://45-126-125-189.cloud-xip.com/github/issues-closed/eXpandFramework/eXpand/RazorView.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ARazorView)
# About 

The `RazorView` module uses the Razor c# syntax to generate views out of Business data. 

## Details
This is a `platform agnostic` module, to generate a view simply create a new `RazorView` business object and configure the available properties as shown below.

![image](https://user-images.githubusercontent.com/159464/139310461-4dba9eda-0633-4975-8bed-2b1871479afd.png)

To customize the rendering per `RazorView` object use the next snippet:

```c#
Application.WhenRazorViewRendering()
    .Where(e => e.Instance.razorView.Name=="Product view")
    .Do(e => e.Handled = e.SetInstance(t => {
        t.renderedView = "<b>template oveerriden</b>";
        return t;
    })).Subscribe();
```

To customize the rendering per `Bussiness object` object use the next snippet:

```c#
Application.WhenRazorViewDataSourceObjectRendering()
    .Where(e => e.Instance.razorView.Name == "Product view"&&e.Instance.razorView.ObjectSpace.GetKeyValue(e.Instance.instance)==(object)1)
    .Do(e => e.Handled = e.SetInstance(t => {
        t.renderedView = "override template for product with Oid==1".ReturnObservable();
        return t;
    })).Subscribe();
```

### Requirements

After installing the nuget package the next attributes should be set in your project.

```xml
<PreserveCompilationReferences>true</PreserveCompilationReferences>
<PreserveCompilationContext>true</PreserveCompilationContext>
```

The `Preview` editor is configured natively to use the `RichTextBoxEditor` which comes when you install the `Xaf 0ffice` module. If you do not install the Xaf Office module the editor will display raw html insteead. As an alternative editor you may wish to try the `MarkupContentEditor` that comes when you install the `Xpand.XAF.Modules.Blazor` package.
### Examples

In the next screencast we create a `Product RazorView` thats lists `ProductNames` and their `Accessories`

<twitter tags="#RazorView #Blazor">

[![Xpand XAF Modules RazorView](https://user-images.githubusercontent.com/159464/139330687-e28673b9-c460-400c-9862-77f161ee0d99.gif)](https://youtu.be/Kn_mkat-oJs)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/Kn_mkat-oJs)

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
 |RazorLight|2.0.0-rc.3
 |System.Reflection.Metadata|5.0.0
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

