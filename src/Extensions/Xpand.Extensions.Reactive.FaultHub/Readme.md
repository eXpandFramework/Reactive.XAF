# About
All projects in this namespace contain only **static classes with extension methods**. 

Use the [XpandPwsh](https://github.com/eXpandFramework/XpandPwsh) To list all the published extension packages:

```ps1
Get-XpandPackages Release XAFExtensions

Id                          Version   Source
--                          -------   ------
Xpand.Extensions.XAF.Xpo    2.201.2.0 Release
Xpand.Extensions            2.201.2.0 Release
Xpand.Extensions.Mono.Cecil 2.201.2.0 Release
Xpand.Extensions.XAF        2.201.2.0 Release
Xpand.Extensions.Reactive   2.201.2.0 Release
```

Similarly if you wish to install them you can do:
```ps1
Get-XpandPackages Release XAFExtensions|Install-Package
```

These packages are consumed from the modules of this repository therefore they are unit tested together with their consumers. 
