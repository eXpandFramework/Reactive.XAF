![](http://45-126-125-189.cloud-xip.com/nuget/v/Xpand.XAF.Modules.Email.svg?&style=flat) ![](http://45-126-125-189.cloud-xip.com/nuget/dt/Xpand.XAF.Modules.Email.svg?&style=flat)

[![GitHub issues](http://45-126-125-189.cloud-xip.com/github/issues/eXpandFramework/expand/Email.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AEmail) [![GitHub close issues](http://45-126-125-189.cloud-xip.com/github/issues-closed/eXpandFramework/eXpand/Email.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AEmail)
# About 

The `Email` module sends your Business Object as email. 

## Details
This is a `platform agnostic` module, that generates an `Email` action using predefined model configurations like the next one.

```xml
<Application>
  <ReactiveModules>
    <Email>
      <EmailAddress>
        <EmailAddress Address="mail@gmail.com" Index="0" />
      </EmailAddress>
      <Recipients>
        <EmailRecipient Id="Admins" Index="0" />
      </Recipients>
      <Rules>
        <EmailRule Id="RazorView rules">
          <ObjectViews>
            <EmailObjectView Id="Preview" Index="0" />
            <EmailObjectView Id="Template" Index="0" />
          </ObjectViews>
          <ViewRecipients>
            <EmailViewRecipient Id="Preview to Admins" Index="0" />
            <EmailViewRecipient Id="Template to Admins" Index="0" />
          </ViewRecipients>
        </EmailRule>
      </Rules>
      <SmtpClients>
        <EmailSmtpClient Id="smtp.gmail.com" Index="0">
          <ReplyTo>
            <EmailAddressesDep Email="mail@gmail.com" Index="0" />
          </ReplyTo>
        </EmailSmtpClient>
      </SmtpClients>
    </Email>
  </ReactiveModules>
</Application>
```

> To avoid sending email twice use the `IModelEmailObjectView.UniqueSend` attribute, which will result a disabled Email action for the match BO instance.  
> 
![image](https://user-images.githubusercontent.com/159464/139561271-138cc53b-41d6-4569-a5e1-2ab65a66aa91.png)

To customize the sending use the next snippet:

```c#
Application.WhenSendingEmail()
    .Do(e => {
        var smtpClient = e.Instance.client; //configure the client
        var mailMessage = e.Instance.message; //configure the message
    }).Subscribe()
```

To customize the rendering per `Bussiness object` object use the next snippet:


### Examples

In the next screencast we create the previous model configuration resulting in the `Email` action activation. Then we test the action by sending an Email in both the `Blazor and the Windows` platforms.   

<twitter tags="#Email #Blazor">

[![Xpand XAF Modules Email](https://user-images.githubusercontent.com/159464/139561334-29a19d4f-085a-43f8-b93f-4c5fdf2aa1a4.gif)](https://youtu.be/Xy3IzZM6HYY)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/Xy3IzZM6HYY)

--- 

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Email`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.EmailModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net6.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.Validation**|**Any**
|[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.222.2
 |Xpand.Extensions.Reactive|4.222.2
 |Xpand.Extensions|4.222.2
 |Xpand.Extensions.XAF|4.222.2
 |Xpand.Extensions.XAF.Xpo|4.222.2
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.222.2
 |Xpand.Patcher|3.0.17
 |System.Reactive|5.0.0
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |Newtonsoft.Json|13.0.1
 |Xpand.Collections|1.0.4
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.222.2

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Email.EmailModule))
```


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Email.Email). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

