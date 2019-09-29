![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.Reactive.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.Reactive.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/Reactive.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Reactive) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Reactive.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Reactive)
# About 

The `Reactive` module can be used to create XAF DSL implementations in a Reactive/Functional style. 

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: `

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|Fasterflect.Xpand|2.0.1
 |System.Interactive|3.2.0
 |System.Reactive|4.1.6
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|1.0.34

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the contructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Reactive.ReactiveModule))
```

## Details
The module does not use controllers but only the existing or new XAF events where they are modeled as in observable with the prefix `When`. 

Observables are nothing more than a Type however that provides operators/methods to combine,merge, zip, observeOn(Scheduler) using LINQ style syntax.

For example to get the first Customer ListView created since your application start you may write.

```cs
ListView listView=await application.WhenListViewCreated().ToListView().When(typeof(Customer))
```
To get the first new Customer created you can write:
```cs
Customer listView=await application.NewObject<Customer>()
```
To add that customer to the first view created collectionsource you can write:
```cs
var listView = application.WhenListViewCreated().ToListView().When(typeof(Customer));
var newCustomer = application.NewObject<Customer>();
await newCustomer.CombineLatest(listView, (customer, view) => {
    view.CollectionSource.Add(customer);
    return customer;
})
```

And so on, the sky is the limit here as you can write custom operators just like a common c# extension method that extends and returns an `IObservable<T>`. All modules of this repository that have a dependency to the Reactive package are developed similar to the above example. Explore their docs, tests, source to find endless examples.






### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Reactive)

### Examples
All Xpand.XAF.Modules that reference this package are developed the RX way.
