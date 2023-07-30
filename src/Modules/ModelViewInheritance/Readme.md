![](http://185-229-225-45.cloud-xip.com/nuget/v/Xpand.XAF.Modules.ModelViewInheritance.svg?&style=flat) ![](http://185-229-225-45.cloud-xip.com/nuget/dt/Xpand.XAF.Modules.ModelViewInheritance.svg?&style=flat)

[![GitHub issues](http://185-229-225-45.cloud-xip.com/github/issues/eXpandFramework/expand/ModelViewInheritance.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AModelViewInheritance) [![GitHub close issues](http://185-229-225-45.cloud-xip.com/github/issues-closed/eXpandFramework/eXpand/ModelViewInheritance.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AModelViewInheritance)
# About 

The `ModuleViewInheritance` changes the default Model View generation without coding.

## Details
This is a `platform agnostic` module that extends the model views nodes with the `IModelObjectViewMergedDifferences` interface to allow model view differences composition. 

![image](https://user-images.githubusercontent.com/159464/50849204-f80e3b00-137e-11e9-8c6c-0a93edffb954.png)

**We want to change the default Model View generation without coding, using the XAF ModelEditor**
</br><u>Traditionally:</u>
Doing such a complex task without building a similar functionality is not possible. You need an engine that will generate model layers out of user predefined rules. You need to have tests and EasyTest for it. In this case that eye cannot do it. 
</br><u>Xpand.XAF.Modules Solution:</u>
The cross platform [Xpand.XAF.Modules.ModuleViewInheritance](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ModelViewInheritance) module `reprograms` the default `design-time` model `view generation` to respect existing view model differences.
 </br>

In the next screencast (Blazor): 
1. We create a new Customer class of type Person
2. We modify the DetailView Layout of our Customer removing ViewItems using the XpandModelEditor
3. We create a Partner class of type Person (Partner != Customer)
4. We open again the XpandModelEditor to verify that Customer Layout is not the same as Partner.
5. We apply an attribute to the Partner and the layouts match
6. We verify in Blazor XAF UI that both views look exactly the same.

<Twitter tags="#Blazor">

[![Xpand XAF Modules ModelViewInheritance Blazor](https://user-images.githubusercontent.com/159464/104476203-a5e4fd80-55c8-11eb-8b75-c62bf5d7c1d7.gif)](https://www.youtube.com/watch?v=6O02vQzNRPs&t=8s)
</Twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/uh4SMPwJ5pU)


In the next screencast (WinForms/WinDesktop/WebForms): 
   1. First we extend the model with the GridView component using the [Xpand.XAF.Modules.ModelMapper](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ModelMapper).
   1. Then, we used the [CloneView](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/CloneModelView) package to clone the `BaseObject_ListView` as a `CommonGridView_ListView`. 
   2. Next, the [Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive) `WhenGeneratingModelNodes` is used to assign the `CommonGridView_ListView` as a `base` view.
   2. Finally, we `modify` the CopyToClipBoard value on the `CommonGridView_ListView` and `check` that is reflected appropriately on the `Customer_ListView`. </br></br>

<Twitter tags="#Winforms #WebForms">

[![jiRSdwmukl](https://user-images.githubusercontent.com/159464/86963022-84640e80-c16c-11ea-8f8d-523a4d6f3312.gif)](https://youtu.be/uh4SMPwJ5pU)

</Twitter>

--- 

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples
Bellow are a few examples of how we use the module in `eXpandFramework`. 



![image](https://user-images.githubusercontent.com/159464/50846982-1709ce80-1379-11e9-877a-6a2e277867a7.png)

to derive a version with `Remember Me` support as below:

![image](https://user-images.githubusercontent.com/159464/50847225-b75ff300-1379-11e9-998d-bcc22bc4bd00.png)

The next `WorldCreator`modified version of `PersistentMemberInfo`:

![image](https://user-images.githubusercontent.com/159464/50848737-af09b700-137d-11e9-94f0-578a0a922455.png)


is used to derive a version for the `PersistentCoreTypeMemberInfo` like:

![image](https://user-images.githubusercontent.com/159464/50848552-399de680-137d-11e9-84dc-a1d574100b48.png)

and in addition one for the `PersistentCollectionMemberInfo` 

![image](https://user-images.githubusercontent.com/159464/50848410-e7f55c00-137c-11e9-8f4a-c9511d95455b.png)


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.ModelViewInheritance`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.ModelViewInheritanceModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net6.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.231.3
 |Xpand.Extensions|4.231.3
 |Xpand.Extensions.Reactive|4.231.3
 |Xpand.Extensions.XAF|4.231.3
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.231.3
 |System.ValueTuple|4.3.0
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |Xpand.Collections|1.0.4
 |Xpand.Patcher|3.0.17
 |System.Reactive|5.0.0
 |System.Text.Json|7.0.2
 |System.Threading.Tasks.Dataflow|7.0.0
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.231.3

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.ModelViewInheritance.ModelViewInheritanceModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.ModelViewInheritance.ModelViewInheritance). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

