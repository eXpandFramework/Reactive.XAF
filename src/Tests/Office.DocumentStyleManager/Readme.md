![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Office.DocumentStyleManager.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Office.DocumentStyleManager.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Office.DocumentStyleManager.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+label%3AOffice.DocumentStyleManager) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Office.DocumentStyleManager.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+label%3AOffice.DocumentStyleManager)
# About 

The DocumentStyleManager can massively re-brand a large number of documents using style templates.

## Details

---

**Credits:** to the Company (wants anonymity) that [sponsor](https://github.com/sponsors/apobekiaris) the initial implementation of this module.

---

This is a `platform agnostic` module that can be used to configure templates that can be applied to XAF domain components that contain members which store RTF documents.

### Model Configuration

* To configure for which members populate the StyleManager SingleChoiceAction you can use the model as in the next image.

  ![image](https://user-images.githubusercontent.com/159464/94731917-34ad7180-036e-11eb-962a-4717cbc90681.png)

* In the `ImportStyles` node you can configure which Domain Components will be used in case you need to import styles from other documents while you generating the template.

   ![image](https://user-images.githubusercontent.com/159464/94734081-5d833600-0371-11eb-9c8c-2ccf44a07cd9.png)

* The `ApplyTemplateListViews` node contain the ListViews where you can apply the generated template.
  
  ![image](https://user-images.githubusercontent.com/159464/94734174-89062080-0371-11eb-9e9b-4222fa896d20.png)
* Additional options are available as DocumentStyManager attributes to help you override the default style properties. 

  ![image](https://user-images.githubusercontent.com/159464/94733854-11d08c80-0371-11eb-8841-610163d19fbb.png)

### Template Generation

The StyLeManager action is available only for Detailviews and will display the next view using the DetailView.CurrentObject and the already discussed model configuration.

![TestApplication Win_TFWorDs5wg](https://user-images.githubusercontent.com/159464/94734788-67596900-0372-11eb-960b-610b70b6bc1b.png)

This is a non persistent view and changes sent back to the owner view when `Accept` is executed. The Owner object gets modified but the transaction not committed.

On the right side we have two RichEditPropertyEditors. The top one will display how the changed and the bottom the original content.

On the left side we have two ListViews with styles from the top-changed document. The `green` color means that the style is used. The `Replacement Styles` contains only styles that match the type (Paragraph/Character) of the `All Styles` view.

The following operations can be performed at the top left editor (use the bottom editor for compare with the original):
1. `Apply Style`: Position the cursor at any place in the top-left editor and change either the paragraph or the character style by selecting from the Apply Styles listview and executing the action.
2. `Delete Styles`: Select one or many styles from the All Styles ListView and execute the action. Additionally execute the DeleteStyles.Unused to remove all unused styles from the top-left editor.
3. `Replace Styles`: Select one or many styles from the All Styles ListView to replace them with the single selection of the Replacement Styles ListView. To persist this operation in a template for later usage activate the Linked styles template and execute the `Template Styles` action.
 ![image](https://user-images.githubusercontent.com/159464/94738015-7393f500-0377-11eb-829f-4c3078fd86e0.png)
4. `Import Styles`: Will display a popup ListView containing the styles parsed from the Domain Components configured in the XAF model as discussed in the configuration section. 
![image](https://user-images.githubusercontent.com/159464/94738189-bb1a8100-0377-11eb-91b5-fdc01ac17eb0.png)



* The subject `Views`, the target container `Calendar` and which Domain Component should be created when a `NewCloudEvent`.</br>
![image](https://user-images.githubusercontent.com/159464/93872067-48a30480-fcd8-11ea-92c7-3512999e53e9.png)
* The CRUD `SynchronizationType` and the `CallDirection`.</br>
![image](https://user-images.githubusercontent.com/159464/93872150-6a03f080-fcd8-11ea-92b0-2289b38032d4.png)
4. `Accept`: Accept the changes to persist the template and sent them to the parent object.

### Apply the generated template to a list of documents.
Navigate to a predefined view that has the `Apply Styles` action enabled as discussed in the configuration section and execute the action to display the Apply Template Style DetailView.

![image](https://user-images.githubusercontent.com/159464/94738868-b904f200-0378-11eb-9372-f0f966062d10.png)

After you select the previously generated template, you can `optionally preview` it and for a final verification by selecting the documents from the left side ListView. On the `Change Styles` view you get a report of the changes that will occur for each document. The changes won't apply unless you execute the `Save Changes` action.

**Possible future improvements:**

Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples

In the screencase we see:

1. First we go note the three documents. We use two different paragraph styles for the two first paragraphs and a character style is applied to a few words of the third paragraph
2. We then open the detailview of one document to create the style modification template that we will apply later to all three documents massively.
3. When the DocumentManager detailview opens we use the `Delete Unused styles` to clear the unwanted styles (optional)
4. To import additional styles we use the ImportStyles action which will parse a predefined list of documents and will extract and display their styles.
5. Then we replace the first paragraph style with its version 2 style. Once the change is previewed and we feel ok with it, we undo the operation and we add the style to the modification template.
6. We perform the steps from 5 again for paragraph 2 and 3
7. We accept the changes and the teample is saved.
8. We select all three documents and use the `Apply Styles` to preview that the template is applied correctly
9. We `SaveChanges`
10. We go again through all document to verify that they now rebranded

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
|**DevExpress.Persistent.Base**|**Any**
 |**DevExpress.ExpressApp**|**Any**
 |**DevExpress.ExpressApp.Xpo**|**Any**
|[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |Google.Apis.Auth|1.49.0
 |Google.Apis.Calendar.v3|1.49.0.2049
 |JetBrains.Annotations|2020.1.0
 |Newtonsoft.Json|12.0.3
 |System.Reactive|4.4.1
 |Xpand.Extensions|2.202.53
 |Xpand.Extensions.Office.Cloud|2.202.54
 |Xpand.Extensions.Reactive|2.202.54
 |Xpand.Extensions.XAF|2.202.54
 |Xpand.Extensions.XAF.Xpo|2.202.50
 |[Xpand.XAF.Modules.Office.Cloud.Google](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Office.Cloud.Google)|2.202.15
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.202.54
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.10

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Office.DocumentStyleManager.Office.Office.DocumentStyleManagerModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Office.Office.DocumentStyleManager.Office.Office.DocumentStyleManager). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

