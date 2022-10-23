![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.SequenceGenerator.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.SequenceGenerator.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/SequenceGenerator.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ASequenceGenerator) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/SequenceGenerator.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ASequenceGenerator)
# About 

The `SequenceGenerator` updates Business Objects members with unique sequential values.

## Details

---

**Credits:** to [Brokero](https://www.brokero.ch/de/startseite/) that [sponsor](https://github.com/sponsors/apobekiaris) the initial implementation of this module.

---

This `platform agnostic` module is a well tested implementation variation of the [E2829](https://supportcenter.devexpress.com/ticket/details/e2829/how-to-generate-a-sequential-number-for-a-persistent-object-within-a-database). The module can be configure to generate unique numerical sequences per ObjectType/memberName combination. 

In details: when any XAF database transaction starts an [Explicit UnitOfWork](https://docs.devexpress.com/XPO/8921/concepts/explicit-units-of-work) is used to acquire a lock to the `SequenceStorage` table. If the table is already locked the it retries until success, if not it queries the table for all the object types that match the objects inside the transaction and assigns their binding members (e.g. a long SequenceNumber member). After the XAF transaction completes with success or with a failure the database lock is released. A long sequential number is generated only one time for new objects.

**Our Invoices and Orders must use unique sequential values in a multi user environment. How can we do it? without sparing my resources?**
</br><u>Traditionally:</u>
This is a non-trivial to implement case without space for mistakes. Therefore a substantial amount of resources is required to research and analyze taking help from existing public work. Do not forget that the requirement is to be super easy to install and use in any project and to be really trustable, so unit and EasyTest is the only way to go in a CI/CD pipeline. 
</br><u>Solution:</u>
The cross platform [Xpand.XAF.Modules.SequenceGenerator](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/SequenceGenerator) generates unique sequential values and provides a XAF UI so the end user can link those values to Business objects members. Its unit and EasyTest run for the latest 3 Major XAF versions with the help of `Xpand.VersionConverter`</br>

</br>In the next screencast we use the XAF UI to create a `subscription` to the `sequence` generator and `assign` the generated sequence to our  `Order.OrderId` configuring the initial sequence to `1000`. Similarly for `Accessory.AccessoryId` where we set the initial value to `2000`. Finally we test by creating an Order and an Accessory where we can `observe` the assigned `values` of OrderId, AccessoryId.

**Blazor**

<twitter tags="#Blazor">

[![Xpand XAF Modules SequenceGenerator Blazor](https://user-images.githubusercontent.com/159464/105914046-74dbe280-6036-11eb-8d32-45c7355311d8.gif)](https://youtu.be/M87TEftU4hU)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/M87TEftU4hU)

---

**WinForms/WinDesktop/WebForms**

<twitter tags="#WinForms #WebForms">

[![hfvTo7UsCI](https://user-images.githubusercontent.com/159464/80309035-f918e500-87da-11ea-8f52-7799457213cf.gif)](https://www.youtube.com/watch?v=t1BDPFU01z8)

</twitter>

The SequenceStorage table is a normal XAF BO, therefore it is possible to create sequence bindings in code by creating instances of that object. However we do not recommend creating instances directly but use the provided API (possibly in a [ModuleUpdater](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater)). The API respects additional constrains and validations.

To generate the configuration of the screencast you can use the next snippet.

```cs
objectSpace.SetSequence<Order>(order => order.OrderID,2000);
objectSpace.SetSequence<Accessory>(accessory => accessory.AccesoryID,1000);
```

To share the same sequence between types use the `SequenceStorage.CustomType` member.

To observe the generation results you may use a call like the next one:

```cs
SequenceGeneratorService.Sequence.OfType<Order>()
.Do(DoWithOrder)
.Subscribe();

SequenceGeneratorService.Sequence.OfType<Accessory>()
.Do(DoWithAccessory)
.Subscribe();
```

---

**Limitations:**

The module works only for MSSQL, MySql, and Oracle databases.

**Possible future improvements:**

1. Provide logic to allow re-generation of a sequence for e.g. when an object is deleted or per demand.
2. Support all database providers.
3. Additional constrains e.g. based on view, on model, on object state etc.
4. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.SequenceGenerator`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.SequenceGeneratorModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: netstandard2.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.Validation**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
 |**DevExpress.ExpressApp.CodeAnalysis**|**Any**
|Xpand.Extensions|4.221.10
 |Xpand.Extensions.Reactive|4.221.10
 |Xpand.Extensions.XAF|4.221.10
 |Xpand.Extensions.XAF.Xpo|4.221.10
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.221.10
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Reactive|5.0.0
 |Newtonsoft.Json|13.0.1
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.221.10

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.SequenceGenerator.SequenceGeneratorModule))
```



### Tests

The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/SequenceGenerator)

