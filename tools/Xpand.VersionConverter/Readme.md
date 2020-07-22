![image](https://user-images.githubusercontent.com/159464/66713086-c8c5a800-edae-11e9-9bc1-73ffc0c215fb.png)

[![image](https://xpandshields.azurewebsites.net/badge/Exclusive%20services%3F-Head%20to%20the%20dashboard-Blue)](https://github.com/sponsors/apobekiaris) ![GitHub stars](https://xpandshields.azurewebsites.net/github/stars/expandframework/devexpress.xaf?label=Star%20the%20project%20if%20you%20think%20it%20deserves%20it&style=social) ![GitHub forks](https://xpandshields.azurewebsites.net/github/forks/expandframework/Devexpress.Xaf?label=Fork%20the%20project%20to%20extend%20and%20contribute&style=social)

![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.VersionConverter.svg?label=nuget.org&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.VersionConverter.svg?style=flat)

# About
The `Xpand.VersionConverter` can your nuget packages to version agnostic.

## Example
**We have a lot of XAF packages compiled against previous XAF versions.  How to reuse them in the latest version without a complex Continuous Integration pipeline**
</br><u>Traditionally:</u>
You have to `support multiple versions` of your projects, so you can `recompile` and `redistribute` each time you want to support a different DX version. You need a complex CI/CD and resources to support it.
</br><u>eXpandFramework Solution:</u>
Use the [Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter) to `patch` your packages on the `fly` in relation to the consuming project `skipping` the need for additional `efforts`.

</br>In the screencast you can see how to make `our company` packages `version agnostic` and be able to produce really valuable compatibility matrixes like: 

[![image](https://user-images.githubusercontent.com/159464/87158168-fbfa8080-c2c7-11ea-9b33-93b67bad7c78.png)](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

The demo is about how to make `MyCompany.MyPackage` DevExpress version agnostic. The process is simple, we add a dependency to `MyPackage.Xpand.VersionConverter` package which was generated with the help of the [New-XpandVersionConverter](https://github.com/eXpandFramework/XpandPwsh/wiki/New-XpandVersionConverter) XpandPwsh cmdlet.</br>

<twitter>

![LgCT4R1ejP](https://user-images.githubusercontent.com/159464/87150508-db77f980-c2ba-11ea-97c0-59c50a52ac0f.gif)

<twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/LvxQ-U_0Sbg)

---

## WorkStream


1. `Xpand.VersionConverter` patch all Xpand assemblies found in your Nuget cache to match the DevExpress version of the current project. This patching occurs before the actual build.
2. If the package fails to detect the DevExpress version due to for e.g to indirect references you can help it with the `DevExpressVersion` MSBuild property. 
2. The patching requires locking so the the patched packages are flagged to avoid locks in subsequent builds. To remove the flags you can use the [Remove-VersionConverterFlags](https://github.com/eXpandFramework/XpandPwsh/wiki/Remove-VersionConverterFlags) XpandPwsh Cmdlet.
3. To troubleshoot you can enable verbose logging you can set the Environmental `VersionConverterVerbose` to 1 and an extensions.log will be created in the package directory.
4. `Xpand.Versionconverter` is already a dependency to all Xpand packages that use DevExpress assemblies in this repository.



### Installation

```ps1
Install-Package Xpand.VersionConverter
```

## Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new/choose)

[![GitHub issues by-label](https://xpandshields.azurewebsites.net/github/issues/expandframework/expand/VersionConverter)](https://github.com/eXpandFramework/eXpand/issues?q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AVersionConverter) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/VersionConverter.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AVersionConverter)
