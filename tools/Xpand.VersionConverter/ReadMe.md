This package contains logic that runs on each build. 

It will modify the DevExpress references in all Xpand.XAF.* assemblies inside $(TargetDir).

The DevExpress assembly reference will change to match the current project DevExpress version.

<sub><sub>The current DX version is detected by first parsing the current project for a FullName DX assembly. If not found it will try in the $(TargetDir) to get the version of the first DX assembly found. </sub></sub>
