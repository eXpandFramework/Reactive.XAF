[![image](https://img.shields.io/badge/Exclusive%20services%3F-Head%20to%20the%20dashboard-Blue)](https://github.com/sponsors/apobekiaris) ![GitHub stars](https://img.shields.io/github/stars/expandframework/devexpress.xaf?label=Star%20the%20project%20if%20you%20think%20it%20deserves%20it&style=social) ![GitHub forks](https://img.shields.io/github/forks/expandframework/Devexpress.Xaf?label=Fork%20the%20project%20to%20extend%20and%20contribute&style=social)

[![image](https://img.shields.io/badge/JOIN-LinkedIn.eXpandFramework.com-blue)](http://linkedin.expandframework.com/) [![image](https://img.shields.io/badge/SUBSRCIBE-YouTube.eXpandFramework.com-blue)](http://youtube.expandframework.com/) [![image](https://img.shields.io/badge/FOLLOW-twitter/eXpandFramework-blue)](https://twitter.com/expandframework/)
# About 

| ![image](https://user-images.githubusercontent.com/159464/66713086-c8c5a800-edae-11e9-9bc1-73ffc0c215fb.png) |  [![Custom badge](https://img.shields.io/endpoint.svg?label=Nuget.org&url=https%3A%2F%2Fxpandnugetstats.azurewebsites.net%2Fapi%2Ftotals%2FXAF)](https://www.nuget.org/packages?q=Xpand.XAF) 
|----------|--------
**Stable**|`nuget.exe list Xpand.XAF`
**Lab**|`nuget.exe list Xpand.XAF -source https://xpandnugetserver.azurewebsites.net/nuget`


In the `Reactive.XAF` repository you can find **low dependency** `DevExpress XAF` **modules** and **tools** distributed only from Nuget.org. 

We aim for low dependency XAF modules so expect to see only a small set of classes per project. To learn more about each module navigate to its root `Readme` file or search the [Wiki](http://xaf.wiki.expandframework.com).

There are three project categories, follow the links to read more. 

2. [Reactive.XAF.Modules](https://github.com/eXpandFramework/XAF/tree/master/src/Modules)
1. [Reactive.XAF.Extensions](https://github.com/eXpandFramework/XAF/blob/master/src/Extensions/)
3. [Tools](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools)


### Compatibility Matrix 

The modules found in this repository are unit tested and EasyTested, consuming the released packages, against the latest Minor of **each Major version** for the last **three years**.



|![Custom badge](https://img.shields.io/endpoint?style=plastic&url=https%3A%2F%2Fxpandnugetstats.azurewebsites.net%2Fapi%2Ftotals%2FXAFBuild%3Findex%3D1%26branch%3Dmaster%26shield%3Dcoverage)| Release  | Lab|
|---|---|---|
|![Custom badge](https://img.shields.io/endpoint?style=for-the-badge&label=%20&url=https%3A%2F%2Fxpandnugetstats.azurewebsites.net%2Fapi%2Ftotals%2FLatestXAFMinors%3Findex%3D1)|![Custom badge](https://img.shields.io/endpoint?style=plastic&url=https%3A%2F%2Fxpandnugetstats.azurewebsites.net%2Fapi%2Ftotals%2FXAFBuild%3Findex%3D1%26branch%3Dmaster%26shield%3Dtests)|![Custom badge](https://img.shields.io/endpoint?&style=plastic&url=https%3A%2F%2Fxpandnugetstats.azurewebsites.net%2Fapi%2Ftotals%2FXAFBuild%3Findex%3D1%26branch%3Dlab%26shield%3Dtests)

## Versioning
The modules are **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The modules follow the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).


### Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Question%2C+XAF&template=xaf--question.md&title=)

![GitHub issues by-label](https://img.shields.io/github/issues/expandframework/expand/Standalone_XAF_Modules.svg) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Standalone_XAF_Modules.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AXAF+)

### Efficient Package Management

Working with many nuget packages may be counter productive. So if you want to boost your productivity make sure you go through the [Efficient Package Management](https://github.com/eXpandFramework/DevExpress.XAF/wiki/Efficient-package-management) wiki page.

### Build-Modify-Contribute

Detailed instruction on how to build can be found in [QuickBuild.ps1](https://github.com/eXpandFramework/Reactive.XAF/blob/master/QuickBuild.ps1).

Related discusions are [QuickBuild](https://github.com/eXpandFramework/eXpand/labels/Quickbuild) `labeled`

