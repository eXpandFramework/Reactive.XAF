![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.ModelMapper.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.ModelMapper.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/ModelMapper.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+ModelMapper) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/ModelMapper.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+ModelMapper)
# About 

The `ModelMapper` allows to control all XAF components from the application model.

## Details
This is a `platform agnostic` module that transforms any type to XAF model format and will extend the model with a simple call like:
```cs
public override void Setup(ApplicationModulesManager moduleManager) {
	base.Setup(moduleManager);
	moduleManager.Extend(new ModelMapperConfiguration(typeof(MyClass),typeof(IModelListView),typeof(AnyModelInterfaceType)) );
}
```
For each mapped type a container interface will be generated and used to extend the model. There are several extension methods you can use to identify the major mapped types for further usage such as extension or population.
```cs
typeToMap.ModelMapperContainerTypes();
typeToMap.ModelTypeName();
typeToMap.ModelMapContainerName();
typeToMap.ModelListType();
typeToMap.ModelListItemType();
//GetRepositoryItemNode, AddRepositoryItemNode, GetControlsItemNode, AddControlsItemNode
var listView = (IModelListView)application.Model.Views["Customer_ListView"];
var listViewColumn = listView.Columns["Name"];
listViewColumn.GetRepositoryItemNode(PredefinedMap.RepositoryItemButtonEdit);
listViewColumn.AddRepositoryItemNode(PredefinedMap.RepositoryItem);

var detailView = (IModelDetailView)application.Model.Views["Customer_DetailView"];
var modelPropertyEditor = detailView.Items["Name"];
modelPropertyEditor.GetRepositoryItemNode(PredefinedMap.RepositoryItemButtonEdit);
modelPropertyEditor.AddRepositoryItemNode(PredefinedMap.RepositoryItem);
```

To extend an existing map you can use:
```cs
public override void Setup(ApplicationModulesManager moduleManager) {
	base.Setup(moduleManager);
	applicationModulesManager.ExtendMap(typeof(MyClass)).Subscribe(_ => _.extenders.Add(_.targetInterface,typeof(IModelSomthing)))
}

```

**Model Editor control on all major components used from XAF**
</br><u>Traditionally:</u>
You need to create model interfaces and extend the model for all component structures. You need to link them with the actual runtime objects and editors and update their values. As this is not a non-trivial case a large number of Unit and EasyTest must run on evert build.
</br><u>Xpand.XAF.Modules Solution:</u>
The cross platform [Xpand.XAF.Modules.ModelMapper](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ModelMapper) ships with `predefined maps` for `all` the common XAF `components` such as Grids, Charts, Tree, Pivot etc.
</br>In the next screencast we see how to `extend` the XAF model with the `GridView` and the `GridColumn` components. Then we used the model editor to modify the model and run the application to test our changes at runtime.</br></br>

<twitter>

[![aYbdUf4HwV](https://user-images.githubusercontent.com/159464/86943203-d1d18300-c14e-11ea-9d68-ee68ff57455f.gif)](https://youtu.be/CkJKEfPhS0M)

<twitter>

[![image](https://user-images.githubusercontent.com/159464/87556331-2fba1980-c6bf-11ea-8a10-e525dda86364.png)](https://youtu.be/CkJKEfPhS0M)

##### Map Generation
Generating maps for many types is costly however it happens in parallel and only once. Afterwards the model interfaces are loaded from the `ModelMapperAssembly.dll` found in path. All mapped types are included in this one assembly even if the map was executed from different modules.

The module will generate automatically the `ModelMapperAssembly.dll` under the following conditions.
1. If the assembly does not exist in path.
2. If the ModelMapper module [ModuleVersionId](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.module.moduleversionid?view=netframework-4.8) changed.
3. If any of the mapped types assembly [ModuleVersionId](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.module.moduleversionid?view=netframework-4.8) changed.
4. If Mapping customization changed. (Read more on How to customize a Map)

It is possible for the map to be outdated if an indirect reference of the participating types has changed. The solution is to simply delete the ModelMapperAssembly.dll from path and let it regenerate.


##### What is mapped
1. All Public or Public Nested Types.
1. All public read/write value types and string properties will be transformed to NullAble types.
2. All public readOnly or read/write Reference properties.
3. All public collections that their type can be detected. (They will not get populated though, it is up to the map author to customize the map as in how to customize the map section).
4. All public type attributes if all their arguments Type are public.
5. All Description attributes public or not, resulting in populating the ModelEditor help panel as in the next shot.
![image](https://user-images.githubusercontent.com/159464/61713414-2c68a800-ad61-11e9-838a-fe58f84719cb.png)

##### What is not mapped
1. Private or internal Types.
2. Attributes where any of their argument type is not public.
3. Properties marked as Obsolete or Browsable false or DesignerSerializationVisibility hidden.
4. Properties that lead to recursion when a new XAF model node is generated (XAF will follow all possible paths and this can recourse).
5. DevExpress DesignTime classes.
5. ReservedPropertyNames, ReservedPropertyTypes, ReservedPropertyInstances (see how to customize a map).
6. Already mapped types.
7. The DefaultValue attribute.
8. Attributes where any of their arguments is a Flag combination (XAF fails to generate such configurations).

##### Predefined maps
The module ships with a large list of maps well tested, ready to use and already integrated with eXpandFramework main modules. 

You are free to install any combination of them like the next snippet which installs the GridView and LayoutView maps:
```cs
public override void Setup(ApplicationModulesManager moduleManager){
	base.Setup(moduleManager);
	moduleManager.Extend(PredefinedMap.AdvBandedGridView,PredefinedMap.BandedGridColumn,PredefinedMap.GridView,PredefinedMap.GridColumn);
}
```
The predefined maps are categorized as:
1. Several `ListEditor maps` extend the IModelListView, IModelColumn and their visibility is bound to the respective ListEditor.

    Editor|ListView | Column 
    ------|---------|----------
    GridListEditor|![image](https://user-images.githubusercontent.com/159464/61733035-a0b64200-ad87-11e9-96cd-9b6d0eacd86b.png) | ![image](https://user-images.githubusercontent.com/159464/61733099-bb88b680-ad87-11e9-8279-11dbcd0f4d1d.png) 
    AdvBandedGridListEditor | ![image](https://user-images.githubusercontent.com/159464/61732015-58962000-ad85-11e9-9a28-b9ea0bb744c5.png) | ![image](https://user-images.githubusercontent.com/159464/61731872-0fde6700-ad85-11e9-845c-54f41692334a.png)
    LayoutViewListEditor | ![image](https://user-images.githubusercontent.com/159464/61733269-191d0300-ad88-11e9-86fb-3c6484613554.png) | ![image](https://user-images.githubusercontent.com/159464/61733304-2afea600-ad88-11e9-920c-e5699a961946.png)
    TreeListEditor|![image](https://user-images.githubusercontent.com/159464/61733920-84b3a000-ad89-11e9-8d60-456589982d3d.png)|![image](https://user-images.githubusercontent.com/159464/61733872-651c7780-ad89-11e9-9b8c-db720fcc33cd.png)</br></br>Navigation</br>![image](https://user-images.githubusercontent.com/159464/61733960-9ac16080-ad89-11e9-88a5-a69fccb07aaa.png)
    ChartListEditor|![image](https://user-images.githubusercontent.com/159464/61734506-b7aa6380-ad8a-11e9-81af-b59d46574f8f.png)|![image](https://user-images.githubusercontent.com/159464/61734558-d14bab00-ad8a-11e9-84d0-036ac96cdbe1.png)
    PivotGridListEditor|![image](https://user-images.githubusercontent.com/159464/61737502-06f39280-ad91-11e9-91b7-0a1118a8d38e.png)|![image](https://user-images.githubusercontent.com/159464/61737545-18d53580-ad91-11e9-9463-929df7505a3c.png)
    SchedulerListEditors(Win/Web)|![image](https://user-images.githubusercontent.com/159464/61739319-dca3d400-ad94-11e9-8c6e-290e707a32c6.png)|![image](https://user-images.githubusercontent.com/159464/61739368-f5ac8500-ad94-11e9-9e40-32286ce6fcbb.png)

2. The `RepositoryItems` map extend only in windows both the IModelColumn and the IModelPropertyEditor interfaces. However since the actual repository is known only in runtime, the extension is of a model list type where multiple map nodes can be added and they will be matched automatically at runtime favoring the node index.</br></br>In this category the following maps exist: 
_RepositoryItem, RepositoryItemTextEdit, RepositoryItemButtonEdit, RepositoryItemComboBox, RepositoryItemDateEdit, RepositoryFieldPicker, RepositoryItemPopupExpressionEdit, RepositoryItemPopupCriteriaEdit, RepositoryItemImageComboBox, RepositoryItemBaseSpinEdit, RepositoryItemSpinEdit, RepositoryItemObjectEdit, RepositoryItemMemoEdit, RepositoryItemLookupEdit, RepositoryItemProtectedContentTextEdit, RepositoryItemBlobBaseEdit, RepositoryItemRtfEditEx, RepositoryItemHyperLinkEdit, RepositoryItemPictureEdit, RepositoryItemCalcEdit, RepositoryItemCheckedComboBoxEdit, RepositoryItemColorEdit, RepositoryItemFontEdit, RepositoryItemLookUpEditBase, RepositoryItemMemoExEdit, RepositoryItemMRUEdit, RepositoryItemBaseProgressBar, RepositoryItemMarqueeProgressBar, RepositoryItemProgressBar, RepositoryItemRadioGroup, RepositoryItemTrackBar, RepositoryItemRangeTrackBar, RepositoryItemTimeEdit, RepositoryItemZoomTrackBar, RepositoryItemImageEdit, RepositoryItemPopupContainerEdit, RepositoryItemPopupBase, RepositoryItemPopupBaseAutoSearchEdit_

    --- | --- 
    ---------|----------
    ![image](https://user-images.githubusercontent.com/159464/61741249-1ecf1480-ad99-11e9-8399-668fbdf6909d.png)| ![image](https://user-images.githubusercontent.com/159464/61741339-5211a380-ad99-11e9-9cef-ca15ac98c0bb.png)

3. The `Controls` maps extends in both platforms the IModelPropertyEditor interface in a design similar to the RepositoryItem maps.<br><br>In this category we find the following maps:
_RichEditControl, DashboardViewer, ASPxDashboard, ASPxHtmlEditor, ASPxUploadControl, ASPxDateEdit, ASPxHyperLink, ASPxLookupDropDownEdit, ASPxLookupFindEdit, ASPxSpinEdit, ASPxTokenBox, ASPxComboBox_
 
    Windows | Web 
    ---------|----------
    ![image](https://user-images.githubusercontent.com/159464/61742550-0ad8e200-ad9c-11e9-8a36-1d4d6bf86aa5.png) | ![image](https://user-images.githubusercontent.com/159464/61742505-f1379a80-ad9b-11e9-9428-fa947c873fb6.png) 
4. `Module specific maps`. In this category we have the DashboardDesigner map which extends the IModelDashboardModule interface as shown:<br>
    ![image](https://user-images.githubusercontent.com/159464/61750194-5ba50680-adad-11e9-94fa-b3c0f76d2e45.png)

5. In the `IModelView related maps `category we find the XafLayoutControl, the SplitContainerControl and the PopupControl maps.

XafLayoutControl | SplitContainerControl 
---------|----------
 ![image](https://user-images.githubusercontent.com/159464/61749379-5050db80-adab-11e9-93de-24ea104aaac7.png)| ![image](https://user-images.githubusercontent.com/159464/61752097-34513800-adb3-11e9-9907-073be2e5843c.png)

**ASPxPopupControl**

DashboardView | ListView | DetailView
---------|----------|---------
![image](https://user-images.githubusercontent.com/159464/61752712-73808880-adb5-11e9-897e-081e8864882f.png)|![image](https://user-images.githubusercontent.com/159464/61752825-ec7fe000-adb5-11e9-8217-2e125f992319.png)|![image](https://user-images.githubusercontent.com/159464/61752720-7f6c4a80-adb5-11e9-832e-931a23b8a159.png)
  
##### <u>How to customize a map</u>
You can customize the map the following ways:
1. On declaration using the ModelMapperConfiguration properties.
    ```cs
        public interface IModelMapperConfiguration{
            string VisibilityCriteria{ get; }
            string ContainerName{ get; }
            string MapName{ get; }
            string DisplayName{ get; }
            string ImageName{ get; }
            List<Type> TargetInterfaceTypes { get; }
            Type TypeToMap{ get; set; }
            bool OmitContainer{ get; }
        }

    ``` 
2. Using static collections of the `TypeMappingService` class such as _ReservedPropertyNames, ReservedPropertyTypes, ReservedPropertyInstances, AdditionalTypesList, AdditionalReferences_
3. Adding or inserting Type or Property related rules. For example to disable the map of all attributes you can write:
    ```cs
    TypeMappingService.PropertyMappingRules.Add(("Disable", tuple => {
        foreach (var modelMapperPropertyInfo in tuple.propertyInfos){
            foreach (var modelMapperCustomAttributeData in modelMapperPropertyInfo.GetCustomAttributesData().ToArray()){
                modelMapperPropertyInfo.RemoveAttributeData(modelMapperCustomAttributeData);
            }
        }
    }));
    ```
Several real world examples are available at [Predefined](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ModelMapper/Services/Predefined) namespace.
##### <u>How to bind a map</u>

All predefined maps are bound to the runtime instance automatically. The module when any view controls are created will search its model for IModelModelMap properties and use them to configure the related instances. It is possible to customize the process as in next snippet:

  ```cs
ModelBindingService.ControlBind.Subscribe(parameter => parameter.Handled = true);
  ```
If you create a custom map you can manually map as:
```cs 
modelNode.BindTo(objectInstance)
```
The `BindTo` method will follow the hierarchy tree respecting the disabled nodes and will update all properties that are not null.

--- 

##### Possible future improvements:

1. Chart Calculated fields [#717](https://github.com/eXpandFramework/eXpand/issues/717).
1. Any other need you may have.

---

## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.ModelMapper`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.ModelMapperModule));
    ```
## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net461`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|Enums.NET|3.0.3
 |Fasterflect.Xpand|2.0.7
 |JetBrains.Annotations|2020.1.0
 |Mono.Cecil|0.11.2
 |System.CodeDom|4.7.0
 |System.Interactive|4.1.1
 |System.Reactive|4.4.1
 |Xpand.Collections|1.0.1
 |Xpand.Extensions|2.202.38
 |Xpand.Extensions.Reactive|2.202.39
 |Xpand.Extensions.XAF|2.202.39
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|2.202.39
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.9

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.ModelMapper.ModelMapperModule))
```


### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/ModelMapper)

