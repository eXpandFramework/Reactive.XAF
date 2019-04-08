[![GitHub stars](https://img.shields.io/github/stars/eXpandFramework/DevExpress.XAF.svg)](https://github.com/eXpandFramework/DevExpress.XAF/stargazers) **Star the project if you think it deserves it.** 

[![GitHub forks](https://img.shields.io/github/forks/eXpandFramework/DevExpress.XAF.svg)](https://github.com/eXpandFramework/DevExpress.XAF/network) **Fork the project to extend and contribute.**

[![Azure DevOps Tests](https://img.shields.io/azure-devops/tests/expandDevOps/expandframework/23.svg?logo=azuredevops)](https://dev.azure.com/eXpandDevOps/eXpandFramework/_build/latest?definitionId=23) [![Azure DevOps Coverage](https://img.shields.io/azure-devops/coverage/eXpandDevOps/expandframework/23.svg?logo=azuredevops)](https://dev.azure.com/eXpandDevOps/eXpandFramework/_build/latest?definitionId=23)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/XAF.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AXAF) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/XAF.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AXAF+)


| <img src="http://expandframework.com/images/site/logo.png" width=150 height=68 alt="eXpandFramework logo"/> | Build | Nuget
|----------|--------|--------
**Stable**|[![Build Status](https://dev.azure.com/eXpandDevOps/eXpandFramework/_apis/build/status/Packages/XAF-Lab?branchName=lab)](https://dev.azure.com/eXpandDevOps/eXpandFramework/_build/latest?definitionId=23?branchName=lab)|`nuget.exe list Xpand.XAF`
**Lab**|[![Build Status](https://dev.azure.com/eXpandDevOps/eXpandFramework/_apis/build/status/Packages/XAF-Lab?branchName=lab)](https://dev.azure.com/eXpandDevOps/eXpandFramework/_build/latest?definitionId=23?branchName=lab)|`nuget.exe list Xpand.XAF -source https://xpandnugetserver.azurewebsites.net/nuget`
<sub><sup>[How do I set up a package source in Visual Studio?](https://go.microsoft.com/fwlink/?linkid=698608)</sup></sub>

# About
In the `DevExpress.XAF` repository you can find **low dependency** DevExpress XAF **modules** distributed as nugets only. 

We aim for low dependency XAF modules so expect to see only a small set of classes per project. To learn more about each module navigate to its root `Readme` file.

There are two namespaces in the source, follow the links to read more. 
1. The [DevExpress.XAF.Extensions](https://github.com/eXpandFramework/XAF/blob/master/src/Extensions/)
2. The [DevExpress.XAF.Modules](https://github.com/eXpandFramework/XAF/tree/master/src/Modules)

## Versioning
The modules are **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The modules follow the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).

### Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new?assignees=apobekiaris&labels=Question%2C+XAF&template=xaf--question.md&title=)
