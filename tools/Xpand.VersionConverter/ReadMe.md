# About
This package with instert logic logic that runs on each build. 

1. **Detects** the current **DevExpress version** by parsing the current project declaration and if not found does a lookup inside project $(TargetDir)
2. **Collects** all DevExpress assembly **references** references in all **Xpand.XAF** assemblies inside project $(TargetDir).
3. **After** each **build** **changes** the **version** in assembly references found.
4. **Changes** the reference **name** if DevExpress references are from **different major**.

Note: For designers support VS uses the original files and not the ones in $(TargetDir), therefore the **original files** inside nuget packages folder will be **patched** as well.

### Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new/choose)