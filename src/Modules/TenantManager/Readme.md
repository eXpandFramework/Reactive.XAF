![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.TenantManager.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.TenantManager.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/TenantManager.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ATenantManager) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/TenantManager.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ATenantManager)
# About 

The `TenantManager` integrates cloud authentication with dedicated datastore Organizations over a certainly controlled management;

## Details
This is a `Blazor` module, to use it you need:

1. An `Organization` and a `User` BO in your solution
2. A dependency between them. A possible implementation is:
 ```cs
    public class ApplicationUser : PermissionPolicyUser, IObjectSpaceLink, ISecurityUserWithLoginInfo {

        [Association("ApplicationOrganization-ApplicationUsers")]
        public XPCollection<ApplicationOrganization> ApplicationOrganizations => GetCollection<ApplicationOrganization>(nameof(ApplicationOrganizations));
    }

    public class ApplicationOrganization:BaseObject{

        [Association("ApplicationOrganization-ApplicationUsers")]
        public XPCollection<ApplicationUser> ApplicationUsers => GetCollection<ApplicationUser>(nameof(ApplicationUsers));

        ApplicationUser _owner;

        public ApplicationUser Owner{
            get => _owner;
            set => SetPropertyValue(nameof(Owner), ref _owner, value);
        }
    }
 ```
 3. A `ConnectionString` property on your `Organization`.
   ```cs
    public class ApplicationOrganization:BaseObject{

        string _connectionString;

        [Size(SizeAttribute.Unlimited)]
        public string ConnectionString{
            get => _connectionString;
            set => SetPropertyValue(nameof(ConnectionString), ref _connectionString, value);
        }
    }
   ```
 4. A non persistent object to display the Organization lookup.
   ```cs
    [DomainComponent]
    public class SelectOrganization:NonPersistentBaseObject{
        public string Message{ get; } = "To get a <b>licence</b> contact Sales.";
        ApplicationOrganization _organization;

        [RuleRequiredField]
        public ApplicationOrganization Organization{
            get => _organization;
            set => SetPropertyValue(nameof(Organization), ref _organization, value);
        }
    }
   ```

5. Register the `Organization` type using the Model Editor and if the design is similar, the rest auto fill (see screencast).

    ![image](https://user-images.githubusercontent.com/159464/154494879-0bf44608-f5cc-4a60-96af-1b0c58946a2a.png)
  
### Workflow
* The `Manager` database controls the User-Organization relationship. The `Manager` admin can assign 1 ownership to a user thus making him `Administrator` in his own `Organization` or he can simply allow participation as a `DefaultRole`. if the user is `Owner` he can create `new users` without help of a Manager Admin. For each new Organization `UserEmail` a new user is created in the Manager DB and is link to the Organization, so the user is ready for login. 
* The user can use for e.g. a B2C flow for authentication, registration etc. Once a new user is authenticated you are responsible for creating him in the manager db as per Solution Wizard sample code.
* The module will create an Organization user, using the same email stored in the Manager DB on user first authentication in the Organization database. A new user in the `Manager` database is created when a new `Organization` user is committed. The `DefaultRoleCriteria`, `AdminRoleCriteria` model attributes will be used to query and link with a role. On each authentication in an `Organization` if the user `owns` then the `AdminRole` will be linked else the `DefaultRole`  
* It is your responsibility to create initial data for all datastore. The `ModuleUpdater` will be called for all datastores. There are cases e.g. you want to create `Organization` only in the Manager db that you may need to differentiate base on the database name.
  ```cs
    if (ObjectSpace.Connection().Database.StartsWith("OrganizationManager")){
        AddOrganization();
    }
  ``` 
* The module assumes that the `Organization` type is used only from the Manager DB so it hides it from all possible places while an Organization.
* The `Organization` holds its `ConnectionString` in a property configured from the model. To override it mark the property `NonBrowsable`, `NonPersistent` and use a snippet like the next one.
    ```c#
    public override void Setup(ApplicationModulesManager moduleManager){
      base.Setup(moduleManager);
      moduleManager.WhenApplication(xafApplication => 
              xafApplication.WhenCustomizeConnectionString<ApplicationOrganization>(organization => organization.ConnectionString))
          .Subscribe(this);
    }

    ```
    
### Examples

In the screencast:
1. I modify the project's `ApplicationOrganization`, `ApplicationUser` declaring a dependecy with each other.
1. I create `SelectOrganization` `prompt` BO/view.
2.  I make the `TenantManager` package aware of my classes with the ModelEditor.
3. I start the app.
1. I attempt to login with my GitHub account and the system responds with `To get a license please contact sales ` markup.
2. A Manager DB login and makes me Owner of Org.
3. I attempt login again with my GitHub account, the system prompts me to choose Organization.
4. I login and confirm that I am Admin and can create users without the need of a Manager Admin.


<twitter tags="#TenantManager #Blazor">

[![Xpand XAF Modules TenantManager](https://user-images.githubusercontent.com/159464/154432425-48be1979-1651-48d4-8845-702439d21e35.gif)](https://youtu.be/O87JjWc2BU0)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/O87JjWc2BU0)

--- 

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.TenantManager`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.TenantManagerModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net6.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.Security**|**Any**
 |**DevExpress.Persistent.Base**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
 |**DevExpress.ExpressApp.Blazor**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.232.1
 |Xpand.Extensions.Reactive|4.232.1
 |Xpand.Extensions|4.232.1
 |Xpand.Extensions.XAF|4.232.1
 |Xpand.Extensions.XAF.Xpo|4.232.1
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.232.1
 |[Xpand.XAF.Modules.Blazor](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Blazor)|4.232.1
 |Xpand.Extensions.Blazor|4.232.1
 |Xpand.Patcher|3.0.22
 |System.Reactive|6.0.0
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Text.Json|7.0.2
 |Xpand.Collections|1.0.4
 |System.Threading.Tasks.Dataflow|7.0.0
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.232.1

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.TenantManager.TenantManagerModule))
```


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.TenantManager.TenantManager). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

