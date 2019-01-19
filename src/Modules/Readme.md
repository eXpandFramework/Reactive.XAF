# About

This namespace `Xpand.XAF.Modules` is used for projects that contain XAF modules. 

You have to install the packages from nuget `Install-Package ModuleName` and then register the module as per XAF docs see [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module). 

For some simple modules distributing them as part of a XAF module may sound overkill, though allows each part of the module author/consume to work transparently, avoid breaking changes etc. 

## Versioning
The modules are **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

### Platform agnostic modules list
1. [ModelViewInheritance](https://github.com/eXpandFramework/XAF/tree/master/src/Modules/Agnostic/ModelViewInheritance)
2. [Reactive](https://github.com/eXpandFramework/XAF/tree/master/src/Modules/Agnostic/Reactive)

### Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new/choose)    