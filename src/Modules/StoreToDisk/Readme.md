![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.StoreToDisk.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.StoreToDisk.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/StoreToDisk.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AStoreToDisk) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/StoreToDisk.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AStoreToDisk)
# About 

The `StoreToDisk` package provides partial and secured serialization for your Business Object

## Details
This is a `platform agnostic` module.

Possible use case: it is common practice when developing with Xaf to drop the database on each major change and let Xaf recreate from scratch. What if you want to add initial secured data? 

You could use the updater and somehome code it there e.g. Azure Vaults or Environmental Variables that point to a file exist only locally etc. But there are many problems with this approach. Think for example that your objects maybe created later and not in the Updater by parsing the results of a web service.

To save your self from such headaches and have your objects initialized correctly you can use the `StoreToDisk` package.

* The model configuration is avaiable in StoreToDisk node:

  ![image](https://user-images.githubusercontent.com/159464/182100662-b4a2ff52-9f3c-455a-afd0-eade61daa4f4.png)

  The `Folder` attribute the is location of the serialized files.

* Each Xaf Bussiness Object serialization is stored seperately in this folder. You can use the next snippet to get the file location for each type.
  ```cs
  typeof(Account).StoreToDiskFileName()
  ```
* To configure which properties are serialized you can use the `StoreToDiskAttribute`
  ```cs
    [StoreToDisk(key:nameof(Name),protection:DataProtectionScope.LocalMachine,properties:nameof(Secret))]
    public class Account:MyBaseObject {
        public string Secret { get; set; }
        public string Name { get; set; }
        [Key]
        public Guid Oid { get; set; }
  ```
  `key:` Is the property that will be used for lookup, in many cases the objects real key cannot be used as it is not fixed across database e.g. a Guid or an autogenerated value. This property will not be used when restoring an object state and is your responsibility to assign it to your objects.

  `protection:` Optionaly you can enrypt your data for your `Machine` or `CurrentLogonUser`.

  `properties:` A collection of properties to be serialized to disk.

* The `StoreToDisk` package logice will `save` and optionally `protect` the previous properties values in a json file stored in the `Folder` when:
  > A XafApplication ObjectSpace is commited or when an ObjectSpaceProvider ObjectSpace or an Updating ObjectSpace (think Updater) is commited. This operation will respect only `NewOrUpdated` objects that are marked with the `StoreToDiskAttribute`
* The `StoreToDisk` package logice will `load` and optionally `unprotect` `Folder` data as previously however it will respect only the `New` objects that their serialized properties have `default` values.


    
### Examples

In the screencast:
1. We create an `Account` that derives from `BaseObject` and already has a Guid `Oid key`. Additionaly we add a `Name` and a `Secret` property.
2. We decorate with the `StoreToDiskAttirbute` to serialize the Secret
3. We run the Windows app (Blazor is exactly the same), create a new Account and set its Name and Secret.
4. We modify the app.config to use a new database and start the app again.
5. We examine the json folder and we see that the data are protected for our Machine.
6. We create a `new Account` and set `only its Name`.
7. The StoreDisk detects the existing record from the name and assigns the Secret property


<twitter tags="#StoreToDisk #Blazor">

[![StoreToDisk](https://user-images.githubusercontent.com/159464/182446841-7261e245-524b-45ab-80df-52079b28b24d.gif)](https://youtu.be/cEku_01kt9M)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/cEku_01kt9M)

--- 

**Update**
29/09/24

The module now stores the data in a database using the `StoreToDisk` connectionstring entry.

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.StoreToDisk`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.StoreToDiskModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net9.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.Persistent.Base**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
|Xpand.Extensions.Reactive|4.251.3
 |Xpand.Extensions|4.251.3
 |Xpand.Extensions.XAF|4.251.3
 |Xpand.Extensions.XAF.Xpo|4.251.3
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.251.3
 |Xpand.Patcher|9.0.0
 |System.Reactive|6.0.0
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Text.Json|9.0.0
 |Xpand.Collections|1.0.4
 |System.Security.Cryptography.ProtectedData|9.0.0
 |System.Threading.Tasks.Dataflow|7.0.0
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.251.3
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.251.3

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.StoreToDisk.StoreToDiskModule))
```


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.StoreToDisk.StoreToDisk). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

