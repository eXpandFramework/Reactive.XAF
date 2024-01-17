![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.Reactive.Rest.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.Reactive.Rest.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/Reactive.Rest.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AReactive.Rest) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Reactive.Rest.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AReactive.Rest)
# About 

The `Reactive.Rest` integrates with any REST Service using a simple declarative process.

## Details

---

**Credits:** to companies that [sponsor](https://github.com/sponsors/apobekiaris) parts of this package.

---

This `Platform agnostic` module is valuable when you want to interact with a remote REST api. The module at its current version works only with non-persistent objects.

There are two attributes (RestOperation, RestProperty) responsible for mapping XAF operation e.g CRUD with the REST api.

### The RestOperationAttribute

* Mark a non persistent object for CRUD operations.

  ```cs
    [DomainComponent]
    [RestOperation(Operation.Delete, "Delete" )]
    [RestOperation(Operation.Create, "Create" )]
    [RestOperation(Operation.Get,"Get" )]
    [RestOperation(Operation.Update, "Update")]
    public class RestOperationObject : NonPersistentBaseObject {
    }
  ```

* Mark a property for an operation. 

  ```cs
        private const string BaseUrl = "/ver1/";
        private const string InstanceUrl = BaseUrl + ("{" + nameof(Oid) + "}/");
        bool _isEnabled;
        [JsonProperty("is_enabled")]
        [RestOperation(nameof(HttpMethod.Post), InstanceUrl + "enable", Criteria = nameof(IsEnabled) + "=true")]
        [RestOperation(nameof(HttpMethod.Post), InstanceUrl + "disable", Criteria = nameof(IsEnabled) + "=false")]
        public bool IsEnabled {
            get => _isEnabled;
            set => SetPropertyValue(ref _isEnabled, value);
        }

  ```

  When committing changes if the `IsEnable==true` then a `POST` request will be send to the `enable` endpoint.

* To send requests from an action, use the next configuration

  ```cs
    [RestActionOperation("{"+nameof(Name)+"}/Act")]
    public class RestOperationObject : NonPersistentBaseObject {
        string _name;
        [Key]
        public string Name {
            get => _name;
            set => SetPropertyValue(ref _name, value);
        }
    }

  ```

### The RestPropertyAttribute

* Mark string array properties to help XAF display and manage them through the UI.

  ```cs
    [RestProperty(nameof(StringArrayList))][InvisibleInAllViews]
    public string[] StringArray {
        get => _stringArray;
        set => SetPropertyValue(ref _stringArray, value);
    }

    public BindingList<ObjectString> StringArrayList { get; private set; }
  ```

* Mark `ObjectString` array properties with the `DataSourcePropertyAttribute` to provide source for the UI lookup.

  ```cs
    [DataSourceProperty(nameof(StringArraySource))]
    public BindingList<ObjectString> StringArrayList { get; private set; }
    [Browsable(false)]
    public ReactiveCollection<ObjectString> StringArraySource => _objectStrings;

    protected override void OnObjectSpaceChanged() {
        base.OnObjectSpaceChanged();
        _objectStrings = new(ObjectSpace) {new ObjectString(nameof(StringArraySource))};
    }
  ```

* Inject a dependency from another property value using the next pattern.

  ```cs
    [RestProperty(nameof(RestOperationObject))]
    public string RestOperationObjectName {
        get => _restOperationObjectName;
        set => SetPropertyValue(ref _restOperationObjectName, value);
    }

    public RestOperationObject RestOperationObject {
        get => _restOperationObject;
        set => SetPropertyValue(ref _restOperationObject, value);
    }
  ```

* Inject a single dependency sending a request.

  ```cs
    [RestProperty(nameof(HttpMethod.Get), "Get" +nameof(RestObjectStats)+ "?id={" +nameof(Name)+ "}")]
    [ExpandObjectMembers(ExpandObjectMembers.Always)]
    [JsonIgnore]
    public RestObjectStats RestObjectStats { get; protected set; }

    public string Name {
        get => _name;
        set => SetPropertyValue(ref _name, value);
    }
  ```

* Inject a dependency collection sending a request

  ```cs
    [RestProperty(nameof(HttpMethod.Get), nameof(ActiveObjects),HandleErrors=true)]
    [JsonIgnore]
    public ReactiveCollection<RestActiveObject> ActiveObjects { get; protected set; }
  ```

### Caching 

Once a request returns a valid response it will be cached for the amount defined from the `PollInterval` property. By default `60sec` are used for all RestOperation, RestProperty `GET` requests.

### ObjectSpace GetObjects

To request the available objects the next code can be used.

```cs
ObjectType[] objectTypes = await objectSpace.RequestAll<ObjectType>();
```

### Authentication

### Examples 

In the screencast you can see how we use the module to map the [3comass.io](https://3commas.io/?c=tc348695) crypto bot REST api service.

<twitter>

[![3Commas](https://user-images.githubusercontent.com/159464/113881541-30eb2380-97c5-11eb-964a-673de3f15872.gif)
](https://youtu.be/0LJ2bM1CfMg)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/m64GpvdwxRc)

---



**Possible future improvements:**

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Reactive.Rest`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Reactive.RestModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net6.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.232.2
 |Xpand.Extensions|4.232.2
 |Xpand.Extensions.Reactive|4.232.2
 |Xpand.Extensions.XAF|4.232.2
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.232.2
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |Xpand.Patcher|3.0.24
 |Microsoft.CSharp|4.7.0
 |System.Reactive|6.0.0
 |System.Text.Json|7.0.2
 |System.Threading.Tasks.Dataflow|7.0.0
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.232.2

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Reactive.Rest.Reactive.RestModule))
```



### Tests

The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Reactive.Rest)

