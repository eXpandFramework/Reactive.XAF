![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.CloneMemberValue.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.CloneMemberValue.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/CloneMemberValue.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+CloneMemberValue) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/CloneMemberValue.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+CloneMemberValue)
# About 

The `CloneMemberValue` module will help you to selectively `clone` Bussiness object `members`.

## Details

This is a `platform agnostic` module extends the `IModelMember` nodes with the `IModelMemberCloneValue` uses the next two strategies:

1. It monitors the `DetailView` construction sequence and projects the result to a `Previous/Current` pair which is then used to clone if the context is valid.
2. It monitors the sequence of new object created from the XAF `ListEditor`, it then projects it similarly to a `Previous/Current` pair.
<twitter>
![image](https://user-images.githubusercontent.com/159464/54979695-7bb5ec00-4fac-11e9-8373-b128982b8bc2.png)
</twitter>

if logging is set to verbose all operations will be logged. 
To observe the cloning operations in code use the next pattern in one of your modules.

```cs
public override void Setup(XafApplication application){
	base.Setup(application);
	CloneMemberValueService.CloneMemberValues
		.Do(DoSomethingForEachMemberValue)
		.TakeUntilDisposingMainWindow()
		.Subscribe();
}

private void DoSomethingForEachMemberValue((IModelObjectView modelObjectView, IMemberInfo MemberInfo, IObjectSpaceLink previousObject, IObjectSpaceLink currentObject) valueTuple){
	throw new NotImplementedException();
}
```

---

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.CloneMemberValue`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.CloneMemberValue.CloneMemberValueModule));
    ```

The module is not integrated with any `eXpandFramework` module. You have to install it as described.

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net461`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|Fasterflect.Xpand|2.0.7
 |JetBrains.Annotations|2020.1.0
 |System.Reactive|4.4.1
 |Xpand.Extensions.Reactive|2.202.45
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.202.45
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.9

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.CloneMemberValue.CloneMemberValueModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/CloneMemberValue)

