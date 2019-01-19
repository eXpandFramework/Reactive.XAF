# About
This package with instert logic logic that runs on each build. 

1. **Detects the current DevExpress version** by parsing the current project declaration and if not found does a lookup inside project $(TargetDir)
2. Collects all DevExpress assembly references references **in all Xpand.XAF assemblies** inside project $(TargetDir).
3. **Changes the reference version** in assembly references found.
4. Change the reference name if DevExpress references are from different major.

### Issues
Use main project [issues](https://github.com/eXpandFramework/eXpand/issues/new/choose)