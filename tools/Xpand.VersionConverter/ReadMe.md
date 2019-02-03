![](https://img.shields.io/nuget/v/Xpand.VersionConverter.svg?label=&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.VersionConverter.svg?style=flat)
# About
This package will insert logic that runs on each build. 

1. **Detects** the current **DevExpress version** by parsing the current project declaration and if not found does a lookup inside project $(TargetDir)
2. **Collects** all DevExpress assembly **references** references in all **Xpand.XAF** assemblies inside project $(TargetDir).
3. **After** each **build** **changes** the **version** in assembly references found.
4. **Changes** the reference **name** if DevExpress references are from **different major**.

Note: For designer support VS uses the original files and not the ones in $(TargetDir), therefore the **original files** inside project's nuget packages folder will be **patched** as well.

### Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new/choose)