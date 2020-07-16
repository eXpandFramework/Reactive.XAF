# Problem solutions from the eXpandFramework/DevExpress.XAF Github repository

1. **Due to the large package number a substantial effort is needed even for simple tasks, like installation, package API discovery and version choosing. How to get the best out of them?**
   </br><u>Traditionally:</u>
      You can `discover and install` the packages `one by one` looking for `incompatibilities` between them, by yourself, in each project you plan to consume them.
   </br><u>eXpandFramework Solution:</u>
    Use `only` the `three` container nuget packages [Xpand.XAF.Core.All](https://www.nuget.org/packages/Xpand.XAF.Core.All), [Xpand.XAF.Win.All](https://www.nuget.org/packages/Xpand.XAF.Win.All), [Xpand.XAF.Web.All](https://www.nuget.org/packages/Xpand.XAF.Web.All). They come with the next benefits:
    * Install only one package per platform with agnostic optional.
    * You will get a `copy-paste` module `registration` snippet. 
    * `All API` from all packages is available in the VS intellisense as soon as you start typing. 
    * You do `not` have to deal with versions `incompatibilities`.
    * No extra dependencies if package API is not used.
    * Only one entry in the Nuget Package Manager Console lists.
    * Only one entry in the Project/References list.

    </br>In the next screencast we see how easy is to install all packages that target the Windows platform. It is recommended to use the Nuget `PackageReference` format. First we install all packages and make a note that a dependency is added for all, then we remove a few installation lines and we make a note how the assembly dependencies reflects only that used API. The assembly reference discovery was done with the help of the XpandPwsh [Get-AssemblyReference](https://github.com/eXpandFramework/XpandPwsh/wiki/Get-AssemblyReference) cmdlet.</br>
    ![Xpand XAF All](https://user-images.githubusercontent.com/159464/86915211-447c3780-c12a-11ea-973d-3096044dc22b.gif)

    ---

1. **We have a lot of XAF packages compiled against previous XAF versions.  How to reuse them in the latest version without a complex Continuous Integration pipeline**
   </br><u>Traditionally:</u>
   You have to `support multiple versions` of your projects, so you can `recompile` and `redistribute` each time you want to support a different DX version. You need a complex CI/CD and resources to support it.
    </br><u>eXpandFramework Solution:</u>
    Use the [Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter) to `patch` your packages on the `fly` in relation to the consuming project `skipping` the need for additional `efforts`.

    </br>In the screencast you can see how to make `our company` packages `version agnostic` and be able to produce really valuable compatibility matrixes like: 
    [![image](https://user-images.githubusercontent.com/159464/87158168-fbfa8080-c2c7-11ea-9b33-93b67bad7c78.png)](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)
    We demo how to make `MyCompany.MyPackage` DX version agnostic. The process is simple, we add a dependency to `MyPackage.Xpand.VersionConverter` package which was generated with the help of the [New-XpandVersionConvreter](https://github.com/eXpandFramework/XpandPwsh/wiki/New-XpandVersionConverter) XpandPwsh cmdlet.</br>

    ![LgCT4R1ejP](https://user-images.githubusercontent.com/159464/87150508-db77f980-c2ba-11ea-97c0-59c50a52ac0f.gif)

    ---

1. **Our Invoices and Orders must use unique sequential values in a multi user environment. How can we do it? without sparing my resources?**
   </br><u>Traditionally:</u>
   This is a non-trivial to implement case without space for mistakes. Therefore a substantial amount of resources is required to research and analyze taking help from existing public work. Do not forget that the requirement is to be super easy to install and use in any project and to be really trustable, so unit and EasyTest is the only way to go in a CI/CD pipeline. 
    </br><u>Solution:</u>
    The cross platform [Xpand.XAF.Modules.SequenceGenerator](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/SequenceGenerator) generates unique sequential values and provides a XAF UI so the end user can link those values to Business objects members. Its unit and EasyTest run for the latest 3 Major XAF versions with the help of `Xpand.VersionConverter`</br>

    </br>In the next screencast we use the XAF UI to create a `subscription` to the `sequence` generator and `assign` the generated sequence to our  `Order.OrderId` configuring the initial sequence to `1000`. Similarly for `Accessory.AccessoryId` where we set the initial value to `2000`. Finally we test by creating an Order and an Accessory where we can `observe` the assigned `values` of OrderId, AccessoryId.

    [![hfvTo7UsCI](https://user-images.githubusercontent.com/159464/80309035-f918e500-87da-11ea-8f52-7799457213cf.gif)](https://www.youtube.com/watch?v=t1BDPFU01z8)

        ---

1. **We want to give our power users control over all major components used from XAF through Model Editor**
</br><u>Traditionally:</u>
You need to create model interfaces and extend the model for all component structures. You need to link them with the actual runtime objects and editors and update their values. As this is not a non-trivial case a large number of Unit and EasyTest must run on evert build.
</br><u>eXpandFrame Solution:</u>
The cross platform [Xpand.XAF.Modules.ModelMapper](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ModelMapper) ships with `predefined maps` for `all` the common XAF `components` such as Grids, Charts, Tree, Pivot etc.
</br>In the next screencast we see how to `extend` the XAF model with the `GridView` and the `GridColumn` components. Then we used the model editor to modify the model and run the application to test our changes at runtime.</br></br>

   ![aYbdUf4HwV](https://user-images.githubusercontent.com/159464/86943203-d1d18300-c14e-11ea-9d68-ee68ff57455f.gif)

    ---

1. **We want to change the default Model View generation without coding, using the XAF ModelEditor**
</br><u>Traditionally:</u>
Doing such a complex task without building a similar functionality is not possible. You need an engine that will generate model layers out of user predefined rules. You need to have tests and EasyTest for it. In this case that eye cannot do it. 
</br><u>expandFrameWork Solution:</u>
The cross platform [Xpand.XAF.Modules.ModuleViewInheritance](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ModelViewInheritance) module `reprograms` the default `design-time` model `view generation` to respect existing view model differences.
 </br>In the next screencast: 
   1. First we extend the model with the GridView component using the [Xpand.XAF.Modules.ModelMapper](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ModelMapper).
   1. Then, we used the [CloneView](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/CloneModelView) package to clone the `BaseObject_ListView` as a `CommonGridView_ListView`. 
   2. Next, the [Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive) `WhenGeneratingModelNodes` is used to assign the `CommonGridView_ListView` as a `base` view.
   2. Finally, we `modify` the CopyToClipBoard value on the `CommonGridView_ListView` and `check` that is reflected appropriately on the `Customer_ListView`. </br></br>
   
   ![jiRSdwmukl](https://user-images.githubusercontent.com/159464/86963022-84640e80-c16c-11ea-8f8d-523a4d6f3312.gif)

   ---
1. **We want the end user to configure the default lookup values for certain views**
</br><u>Traditionally:</u>
You have to declare tables that hold which value was for which lookup. To configure it you need to extend the model. As always you have to test support and distribute.
</br><u>eXpandFramework solution:</u>
Use the cross platform [Xpand.XAF.Modules.ViewItemValue](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/ViewItemValue)

   </br> In the screencast we configure the model to allow end user to choose the default values for `Product` and `Order` when he is on the `Order_DetailView`
   [![kMok40PDFn](https://user-images.githubusercontent.com/159464/83734915-4e58d980-a658-11ea-90db-c05fa9f614ac.gif)](https://www.youtube.com/watch?v=90MzTKyVlsg&t=21s)

1. **We want the end user to create persistent (through application restarts) configurations  on how objects are positioned in a ListView**
</br><u>Traditionally:</u>
ThHe implementation outline for this one is to store the object type instances locations in a independent table together with all related info. In addition you have to synchronize actions and movements up/down. Do not forget to write tests and run them again on the next update before you distribute.
</br><u>eXpandFramework solution:</u>
Use the cross platform [Xpand.XAF.Modules.PositionInListView](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/PositionInListView)


   </br>In the screencast we create three customers at runtime and demo the feature by executing the MoveUp/MoveDown actions and close/reopen the view`. At the bottom the [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) is reporting as the module is used
   [![sqFoseHS2q](https://user-images.githubusercontent.com/159464/82759129-e4d50180-9df3-11ea-8bb9-eb6b36452c51.gif)](https://www.youtube.com/watch?v=JBoVNXo19ek)

1. **We want to create a persistent authentication against Azure Active Directory and integrate our apps with the MSGraph endpoints**
</br><u>Traditionally:</u>
Suppose your experience is really great with XAF Win and Web you understand Azure, OAth2, it is natural for you to code asynchronous solutions and you have a lot of experience on code architecture then it should be just time consuming to approach an abstract solution to it. As always remember to make sure it works with the next update. 
</br><u>Solution:</u>
Use the cross platform [Xpand.XAF.Modules.Office.Cloud.Microsoft](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Office.Cloud.Microsoft) authenticates against Azure Active Directory and provides API for querying the MSGraph endpoints.</br>
Below is a demonstration of the package authenticating against `AAD` for both `Win/Web`. Also the API is used to call the `MSGraph` [Me](https://docs.microsoft.com/en-us/graph/api/user-get?view=graph-rest-1.0&tabs=http) endpoint for displaying the authenticated user info in a XAF view. At the bottom the [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive.Logger.Client.Win) is reporting as the module is used. This demo it is Easytested [with this script](https://github.com/eXpandFramework/DevExpress.XAF/blob/master/src/Tests/ALL/CommonFiles/MicrosoftService.cs) for the last three XAF major versions, compliments of the `Xpand.VersionConverter` as described in #2</br></br>

   [![Xpand XAF Modules Office Cloud Microsoft](https://user-images.githubusercontent.com/159464/86131887-e24e8180-baee-11ea-8c02-b64b2c639b6d.gif)](https://www.youtube.com/watch?v=XIczKjE2sFw)

For more examples and details navigate the `eXpandFramework/DevExpress.XAF` [wiki](https://github.com/eXpandFramework/DevExpress.XAF/wiki)