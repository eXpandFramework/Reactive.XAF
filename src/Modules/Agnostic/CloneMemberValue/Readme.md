![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.CloneMemberValue.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.CloneMemberValue.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/CloneMemberValue.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+CloneMemberValue) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/CloneMemberValue.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+CloneMemberValue)
# About 

The `CloneMemberValue` module will help you to selectively `clone` Bussiness object `members`. The application model can be used to `define` the cloning `context` (Views/Members). 

The module uses the next two strategies:
1. It monitors the `DetailView` construction sequence and projects the result to a `Previous/Current` pair which is then used to clone if the context is valid.
2. It monitors the sequence of new object created from the XAF `ListEditor`, it then projects it similarly to a `Previous/Current` pair.
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
`.NetFramework: v4.6.1`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|1.0.16
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Agnostic/Xpand.XAF.Modules.Reactive)|1.2.15
 |fasterflect|2.1.3
 |System.Reactive|4.1.3
 |System.Runtime.CompilerServices.Unsafe|4.5.2
 |System.Threading.Tasks.Extensions|4.5.2
 |System.ValueTuple|4.5.0

## Issues
For [Bugs](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Bug%2C+Standalone_XAF_Modules,+CloneMemberValue&template=standalone-xaf-modules--bug-report.md&title=), [Questions](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Question%2C+Standalone_XAF_Modules,+CloneMemberValue&template=standalone-xaf-modules--question.md&title=) or [Suggestions](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Enhancement%2C+Standalone_XAF_Modules,+CloneMemberValue&template=standalone-xaf-modules--feature-request.md&title=) use main project issues.
## Details
The module extends the `IModelMember` nodes with the `IModelMemberCloneValue`. 

![image](https://user-images.githubusercontent.com/159464/54979695-7bb5ec00-4fac-11e9-8373-b128982b8bc2.png)


if logging is set to verbose all operations will be logged. 
To obesrve the cloning operations in code use the next pattern in one of your modules.

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


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Modules/CloneMemberValue)
