![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Office.DocumentStyleManager.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Office.DocumentStyleManager.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Office.DocumentStyleManager.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AOffice.DocumentStyleManager) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Office.DocumentStyleManager.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AOffice.DocumentStyleManager)
# About 

The DocumentStyleManager can re-brand a large number of documents using style templates.

## Details

---

**Credits:** to the Company (wants anonymity) that [sponsor](https://github.com/sponsors/apobekiaris) the initial implementation of this module.

---

This is a `platform agnostic` module that you can use to configure style replacement templates. Applying them to XAF domain components with RTF document members results in document re-branding.

### Model Configuration

* To configure which members populate the `StyleManager` SingleChoiceAction you can use the model as in the next image.

  ![image](https://user-images.githubusercontent.com/159464/94731917-34ad7180-036e-11eb-962a-4717cbc90681.png)

* In the `ImportStyles` node (see previous image) you can configure which Domain Components will be used in case you need to import styles from other documents while you generating the template.

   ![image](https://user-images.githubusercontent.com/159464/94734081-5d833600-0371-11eb-9c8c-2ccf44a07cd9.png)

* The `ApplyTemplateListViews` node contains the ListViews where the generated template can be applied. A SimpleAction `Apply Styles` with be activated only for there.
  
  ![image](https://user-images.githubusercontent.com/159464/94734174-89062080-0371-11eb-9e9b-4222fa896d20.png)
* Additional options are available as DocumentStyManager attributes. They help you override the default style properties. When a new RTF style is created and properties not set then default properties are used and they affect the style behavior.

  ![image](https://user-images.githubusercontent.com/159464/94733854-11d08c80-0371-11eb-8841-610163d19fbb.png)

### Template Generation

The `StyLeManager` SingleChoiceAction is available only for Detailviews. Execute it to display the view in the next image in a popup WIndow using the DetailView.CurrentObject. 

![TestApplication Win_TFWorDs5wg](https://user-images.githubusercontent.com/159464/94734788-67596900-0372-11eb-960b-610b70b6bc1b.png)

This is a non persistent view and changes sent back to the owner view when `Accept` is executed. The Owner object gets modified but the transaction not committed.

On the right side we have two RichEditPropertyEditors. The top one displays the changed content and the bottom the original.

On the left side we have two ListViews populated with styles from the top-changed document. The `green` color means that the style is used. The `Replacement Styles` contains only styles that match the type (Paragraph/Character) of the `All Styles` selected object.

The following operations can be performed at the top left editor (use the bottom editor for compare with the original):

1. `Apply Style`: Position the cursor at any place in the top-left editor and change either the paragraph or the character style by selecting from the Apply Styles ListView and executing the action.
2. `Delete Styles`: Select one or many styles from the All Styles ListView and execute the action. Additionally execute the `DeleteStyles.Unused` to remove all unused styles from the top-left editor.
3. `Replace Styles`: Select one or many styles from the All Styles ListView to replace them with the single selection of the Replacement Styles ListView. To persist this operation in a template for later usage activate the Linked styles template and execute the `Template Styles` action.
 ![image](https://user-images.githubusercontent.com/159464/94738015-7393f500-0377-11eb-829f-4c3078fd86e0.png)
4. `Import Styles`: Display a popup ListView containing the styles parsed from the Domain Components configured in the XAF model as discussed in the configuration section. 
![image](https://user-images.githubusercontent.com/159464/94738189-bb1a8100-0377-11eb-91b5-fdc01ac17eb0.png)
5. `Accept`: Accept the changes to persist the template. In addition this action will sent any changes of the top-left editor back to the owner Domain Component member.

### Apply the generated template to a list of documents.

Navigate to a predefined view that has the `Apply Styles` action enabled as discussed in the configuration section and execute the action to display the Apply Template Style DetailView.

![image](https://user-images.githubusercontent.com/159464/94738868-b904f200-0378-11eb-9372-f0f966062d10.png)

After you select the previously generated template, you can `optionally preview` it and for a final verification by selecting the documents from the left side ListView. On the `Change Styles` view you get a report of the changes that will occur for each document. The changes won't apply unless you execute the `Save Changes` action.

**Possible future improvements:**

Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples

In the screencast we see:

1. First we note the three documents by navigating through each of them. For the two first paragraphs, two different paragraph styles are used. In the third paragraph a character style is applied to a few words. Next, we will generate a template to re-brand these documents.
2. The `Style Manager` SingleChoice action is configured to activate in the DetailView of the `DocumentObject` type. So we open this DetailView and execute the action..
3. When the DocumentManager DetailView opens we use the `Delete Unused styles` to clear the unwanted styles (optional).
4. To import additional styles we use the `ImportStyles` action which will parse a predefined list of documents and will extract and display their styles.
5. Then we replace the first paragraph style with its version 2 style (Quote->QuoteV2). Once the change is previewed and we feel ok with it, we undo the operation and we add the style to the modification template.
6. We perform the steps from 5 again for paragraph 2 and 3
7. We accept the changes and the template is saved.
8. We select all three documents and use the `Apply Styles` to preview that the template is applied correctly
9. We `SaveChanges`
10. We go again through all document to verify that they now re-branded.

At the bottom the [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) is reporting as the module is used.
 


<twitter>

[![Xpand XAF Modules DocumentStyleManager](https://user-images.githubusercontent.com/159464/94597297-1116f800-0296-11eb-8d88-1938d7286a67.gif)](https://youtu.be/Hbzgfad9yVk)

</twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/Hbzgfad9yVk)


## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Office.DocumentStyleManager`.

    The above only references the dependencies and next steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Office.DocumentStyleManagerModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.Google.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net461`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp.Office**|**Any**
 |**DevExpress.ExpressApp.ConditionalAppearance**|**Any**
|Xpand.Extensions|4.211.4
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|4.211.4
 |[Xpand.XAF.Modules.SuppressConfirmation](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.SuppressConfirmation)|4.211.4
 |[Xpand.XAF.Modules.HideToolBar](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.HideToolBar)|4.211.4
 |[Xpand.XAF.Modules.ViewItemValue](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Xpand.XAF.Modules.ViewItemValue)|4.211.4
 |Xpand.Extensions.Reactive|4.211.4
 |Xpand.Extensions.XAF|4.211.4
 |Xpand.Extensions.XAF.Xpo|4.211.4
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |JetBrains.Annotations|2021.1.0
 |System.Reactive|5.0.0
 |Newtonsoft.Json|13.0.1
 |System.Interactive|5.0.0
 |Microsoft.CodeAnalysis.CSharp|3.10.0
 |System.CodeDom|5.0.0
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)|4.211.4

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Office.DocumentStyleManager.Office.Office.DocumentStyleManagerModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Office.Office.DocumentStyleManager.Office.Office.DocumentStyleManager). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

