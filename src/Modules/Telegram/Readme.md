![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.Telegram.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.Telegram.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/Telegram.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ATelegram) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Telegram.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3ATelegram)
# About

The `Telegram` module provides a reactive, controller-less engine for integrating the Telegram messaging platform with XAF applications.

## Details
This is a `windows` module that allows you to create and manage `TelegramBot` business objects to interact with the Telegram API. The module handles receiving updates, managing chats, and sending messages.

The module provides several business objects to configure and monitor bot interactions:
*   **`TelegramBot`**: The central configuration object. You provide the bot's secret token here and can activate or deactivate it. It serves as a container for its associated chats, commands, and message templates.
*   **`TelegramChat`**: Automatically created for each user who interacts with your bot. It can be deactivated to block communication with a specific user.
*   **`TelegramBotCommand`**: Defines the custom commands (e.g., `/help`, `/support`) that your bot will respond to.
*   **`TelegramBotMessageTemplate`**: Allows you to configure predefined replies for specific events, such as the initial `/start` message or a `/stop` message.
*   **`TelegramBotWorkflowCommand`**: This is the integration point with the `Workflow` module. It allows any `CommandSuite` to use a configured `TelegramBot` as an output channel, enabling workflows to send their results as Telegram messages.

<twitter>

The following example demonstrates how to create a bot and a simple workflow that sends a "Hello World" message to all active chats every 10 seconds.

```csharp
    
    // Configure the Telegram Bot
    var bot = ObjectSpace.CreateObject<TelegramBot>();
    bot.Name = "MyNotifierBot";
    bot.Secret = "YOUR_TELEGRAM_BOT_SECRET";
    bot.Active = true;

    // Create the Workflow
    var suite = ObjectSpace.CreateObject<CommandSuite>();
    suite.Name = "My Telegram Workflow";

    var ticker = ObjectSpace.CreateObject<TimeIntervalWorkflowCommand>();
    ticker.Interval = TimeSpan.FromSeconds(10);
    ticker.CommandSuite = suite;

    var telegramMessenger = ObjectSpace.CreateObject<TelegramBotWorkflowCommand>();
    telegramMessenger.TelegramBot = bot;
    telegramMessenger.StartAction = ticker; // Triggered by the timer
    telegramMessenger.CommandSuite = suite;
    
    // The TestCommand will provide the message content
    var messageProvider = ObjectSpace.CreateObject<TestCommand>();
    messageProvider.OutputMessages = "Hello World";
    messageProvider.StartAction = ticker;
    messageProvider.CommandSuite = suite;

    telegramMessenger.StartAction = messageProvider; // Chain the telegram command to the message provider

    ObjectSpace.CommitChanges();
    
```

</twitter>

The `Telegram` module's behavior is guaranteed by a comprehensive suite of automated tests. The following sections detail the module's functionality.

#### Core Bot & Chat Management
*   **Bot Connection:** Active `TelegramBot` objects automatically connect to the Telegram API to receive updates.
*   **Chat & User Creation:** When a new user starts a conversation with the bot, `TelegramChat` and `TelegramUser` objects are created and persisted automatically.
*   **Message Handling:** Incoming messages are saved as `TelegramChatMessage` records, linked to the corresponding chat.
*   **Command Handling:** The module parses incoming messages for commands (e.g., `/help`) and can be extended to trigger custom logic.
*   **Automated Replies:** The system automatically sends pre-configured messages from `TelegramBotMessageTemplate` when users send `/start` or `/stop` commands, which also toggles the `Active` status of their `TelegramChat`.

#### Workflow Integration (`TelegramBotWorkflowCommand`)
*   **Message Transmission:** The command sends the string representation of its input objects as messages to all active chats of the configured `TelegramBot`.
*   **Resilience:**
    *   The command fails gracefully and publishes a fault if the configured bot is inactive, has an invalid secret, or encounters a network error.
    *   It tolerates failures for individual chats (e.g., if a user has blocked the bot). The system will attempt to send to all other active chats and will automatically deactivate the single failing chat.
*   **Input Handling:**
    *   The command executes only if it receives one or more non-null input objects from a preceding command. It completes silently if the input is empty.
    *   It handles multiple input objects by sending each one as a separate message.

    To leverage the `TelegramBotWorkflowCommand`, the `Workflow` module must also be explicitly registered in your application, as it is a required dependency for this functionality.
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Workflow.WorkflowModule));
    ```


## Installation
1.  First you need the nuget package so issue this command to the `VS Nuget package console`

    `Install-Package Xpand.XAF.Modules.Telegram`.

    The above only references the dependencies and nexts steps are mandatory.

2.  [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
    or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Telegram.TelegramModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net10.0-windows7.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
 |**DevExpress.ExpressApp.Win**|**Any**
 |**DevExpress.ExpressApp.Validation**|**Any**
|Xpand.Extensions.Reactive|4.252.1
 |Xpand.Extensions.XAF|4.252.1
 |Xpand.Extensions.XAF.Xpo|4.252.1
 |Xpand.Extensions|4.252.1
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.252.1
 |[Xpand.XAF.Modules.Workflow](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Workflow)|4.252.1
 |[Xpand.XAF.Modules.CloneModelView](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.CloneModelView)|4.252.1
 |[Xpand.XAF.Modules.SuppressConfirmation](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.SuppressConfirmation)|4.252.1
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Reactive|6.0.1
 |Lib.Harmony|2.4.2
 |Telegram.Bot|22.7.6
 |Humanizer|3.0.1
 |Microsoft.Extensions.Options|10.0.1
 |Microsoft.Extensions.DependencyInjection.Abstractions|10.0.1
 |Microsoft.CodeAnalysis|5.0.0
 |Microsoft.CodeAnalysis.CSharp|5.0.0
 |Microsoft.Extensions.Configuration.Abstractions|10.0.1
 |Microsoft.Extensions.FileProviders.Abstractions|10.0.1
 |Microsoft.Extensions.Diagnostics.Abstractions|10.0.1
 |Microsoft.Extensions.Logging.Abstractions|10.0.1
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.252.1
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.252.1

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Telegram.TelegramModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Telegram.Telegram).
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

