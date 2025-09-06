![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.ViewItemValue.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.ViewItemValue.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/ViewItemValue.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AViewItemValue) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/ViewItemValue.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AViewItemValue)
# About 

The `ViewItemValue` helps end-users to configure the default values for lookup view items.

## Details
---

**Credits:** to the Company (wants anonymity) that [sponsor](https://github.com/sponsors/apobekiaris) the initial implementation of this module. 

---
This is a `platform agnostic` module. The end-user can execute the `ViewItemValue` action and select which value a lookup property editor will get when a new object is created. To configure the participating DetailView and lookup members you can use the Model Editor as in the next image:

![image](https://user-images.githubusercontent.com/159464/83668842-49a11080-a5d9-11ea-840c-ba8ffec00cca.png)


When the above `Order_DetailView` created the `ViewItemValue` will be active and will contain two items the `Product` and the `Accessory`. Executing the action will result in saving the related lookup editor object key value along with the view and member name in the database. When the same Order_DetailView is created later for a new object then the info from the database will be used to assign the appropriate object.


 **We want the end user to configure the default lookup values for certain views**
</br><u>Traditionally:</u>
You have to declare tables that hold which value was for which lookup. To configure it you need to extend the model. As always you have to test support and distribute.
</br><u>eXpandFramework solution:</u>
Use the cross platform [Xpand.XAF.Modules.ViewItemValue](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ViewItemValue)

</br> In the screencast we configure the model to allow end user to choose the default values for `Product` and `Order` when he is on the `Order_DetailView`

<twitter>

[![kMok40PDFn](https://user-images.githubusercontent.com/159464/83734915-4e58d980-a658-11ea-90db-c05fa9f614ac.gif)](https://www.youtube.com/watch?v=90MzTKyVlsg&t=21s)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://www.youtube.com/watch?v=90MzTKyVlsg&t=21s)

---

**Possible future improvements:**

1. Multiple default object info persistent with one go.
1. ListView support.
2. Remember value strategies e.g Last.
4. Conditional strategies.
5. Scripting strategies.
6. Support for any member type. # done 25.09.22
3. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.ViewItemValue`.

    The above only references the dependencies and next steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.ViewItemValueModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net9.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.Validation**|**Any**
|Xpand.Extensions.Reactive|4.251.5.1
 |Xpand.Extensions.XAF|4.251.5.1
 |Xpand.Extensions|4.251.5.1
 |Xpand.Extensions.XAF.Xpo|4.251.5.1
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.251.5.1
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Reactive|6.0.1
 |System.Interactive|6.0.1
 |System.Text.Json|9.0.0
 |System.Threading.Tasks.Dataflow|7.0.0
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.251.5.1
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.251.5.1

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.ViewItemValue.ViewItemValueModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.ViewItemValue.ViewItemValue). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

