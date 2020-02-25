![](https://img.shields.io/nuget/v/Xpand.VersionConverter.svg?label=nuget.org&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.VersionConverter.svg?style=flat)

# About
This package modifies the DevExpress references in all Xpand.XAF.* assemblies to match the target project DevExpress version.

## Example
3 years ago I develop a my cool 5 xaf modules about X domain. I used the `Xpand.VersionConverter` nuget package, I compiled them and publish on Nuget.
 
3 years passed and I want to use them in a new project. I just need to install the nuget packages and they will work even if DevExpress assemblies names change every year (Major builds). This is the result of Xpand.VersionConverter that patches the version on each build. The **support cost** for my XAF modules closes to **zero**.
 
The alternative is to have a full Continuous Integration pipeline and support it (VERY COSTLY), a version strategy bound to DevExpress versioning just for being able to republish the packages. 

You might say that do not even know whats a CI (Continuous Integration) or perhaps I do not use a CI, but think again, storing the project, taking backups, git repository, open visual studio, Ctrl+F5 is actually a CI.
## Technicals

1. `Xpand.VersionConverter` before the consumer project is build will change the version of all assemblies in the host package to match the DevExpress version of the consumer project. 
2. If the package fails to detect the DevExpress version used from the consumer project due for e.g to indirect references you can help it with the DevExpressVersion msbuild property. 
2. The patching requires locking so the the patched packages are flagged to avoid locks in subsequent builds. To remove the flags you can use the [Remove-VersionConverterFlags](https://github.com/eXpandFramework/XpandPwsh/wiki/Remove-VersionConverterFlags) cmdlet.
3. To troubleshoot you can enable verbose logging you can set the Environmental VersionConverterVerbose to 1 and an extensions.log will be created in the package directory.
4. Xpand.Versionconverter is already a dependency to packages that use DevExpress assemblies in this repository.

### Installation

```ps1
Install-Package Xpand.VersionConverter
```

## Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new/choose)

[![GitHub issues by-label](https://img.shields.io/github/issues/expandframework/expand/VersionConverter)](https://github.com/eXpandFramework/eXpand/issues?q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AVersionConverter) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/VersionConverter.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AVersionConverter)