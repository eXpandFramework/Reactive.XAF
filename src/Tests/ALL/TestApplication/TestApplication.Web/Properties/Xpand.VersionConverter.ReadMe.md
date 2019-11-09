![](https://img.shields.io/nuget/v/Xpand.VersionConverter.svg?label=nuget.org&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.VersionConverter.svg?style=flat)
# Example
3 years ago I develop a my cool 5 xaf modules about X domain. I used the `Xpand.VersionConverter` nuget package, I compiled them and publish on Nuget.
 
3 years passed and I want to use them in a new project. I just need to install the nuget packages and they will work even if DevExpress assemblies names change every year (Major nuilds). This is the result of Xpand.VersionConverter that patches the version on each build. The **support cost** for my XAF modules closes to **zero**.
 
The alternative is to have a full Continuous Integration pipeline and support it (VERY COSTLY), a version strategy bound to DevExpress versioning just for being able to republish the packages. 

You might say that do not even know whats a CI (Continuous Integration) or perhaps I do not use a CI, but think again, storing the project, taking backups, git repository, open visual studio, Ctrl+F5 is actually a CI.
## Technicals
This package will insert logic that runs on each build and it will. 

1. **Detects** the current **DevExpress version** by parsing the current project declaration and if not found does a lookup inside project $(TargetDir)
2. **Collects** all DevExpress assembly **references** references in all **Xpand.XAF** assemblies inside project $(TargetDir).
3. **After** each **build** **changes** the **version** in assembly references found.
4. **Changes** the reference **name** if DevExpress references are from **different major**.

Note: For designer support VS uses the original files and not the ones in $(TargetDir), therefore the **original files** inside project's nuget packages folder will be **patched** as well.

### Installation

```ps1
Install-Package Xpand.VersionConverter
```

## Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new/choose)