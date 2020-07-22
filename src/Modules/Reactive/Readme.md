![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.Reactive.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.Reactive.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/Reactive.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Reactive) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/Reactive.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Reactive)
# About 

The `Reactive` module provides a XAF DSL API for functional/stateless implementations. 

## Details
This is a `platform agnostic` module. All modules that use it as a dependency **do not use controllers**. Instead they use existing or new XAF events where they are modeled as operators with the prefix `When`. 

Observables are nothing more than a Type which provides operators/methods to [CombineLatest](http://reactivex.io/documentation/operators/combinelatest.html), [Merge](http://reactivex.io/documentation/operators/Merge.html), [Zip](http://reactivex.io/documentation/operators/zip.html), [ObserveOn](http://reactivex.io/documentation/operators/combinelatest.html)(Scheduler) using LINQ style syntax see ([ReactiveX.IO](http://reactivex.io/documentation/operators.html)).

An operator is an c# extension method. Such methods are static and best practice is to designed them without storing state as they may be concurrent. To pass the state we use the operator (method) arguments and it return value. Existing operators are more than enough to handle any case and you should avoid custom `IObservable<T>`implementation as they may misbehave when concurrency introduced. 

For more details see [RX Contract](http://reactivex.io/documentation/contract.html).

For example to create an operator that emits when a XAF ListView created you may write:

```cs
public static IObservable<(XafApplication application, ListViewCreatedEventArgs e)> WhenListViewCreated(this XafApplication application){
    return Observable
        .FromEventPattern<EventHandler<ListViewCreatedEventArgs>, ListViewCreatedEventArgs>(
            h => application.ListViewCreated += h, h => application.ListViewCreated -= h)
        .TransformPattern<ListViewCreatedEventArgs, XafApplication>().TraceRX();
}

```
The above `WhenListViewCreated` operator returns an `IObservable<(XafApplication application, ListViewCreatedEventArgs e)` which may be not so handy in some cases. So let's write another one that projects that type to a ListView
```cs
public static IObservable<ListView> ToListView(this IObservable<(XafApplication application, ListViewCreatedEventArgs e)> source){
    return source.Select(_ => _.e.ListView);
}

```
As you can see I used as input the output of the WhenListViewCreated and with the `Select` I can project it to any type I like. System.ValueTuple is a great help here as you most probably want to avoid polluting your code base with classes that do noting more than storing state in a property.

The `Xpand.XAF.Modules.Reactive` modules already provides a great number of operators found at [Xpand.XAF.Modules.Reactive.Services](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Reactive/Services) namespace.

<twitter>

Now let's use the previous two operators to await the first Customer ListView created since our application start.

```cs
ListView listView=await application.WhenListViewCreated().ToListView().When(typeof(Customer))
```
Similarly to get the first new Customer created we can write:
```cs
Customer listView=await application.NewObject<Customer>()
```
Finally let's combine those await so to add that customer to the CollectionSource of the first view created:
```cs
var listView = application.WhenListViewCreated().ToListView().When(typeof(Customer));
var newCustomer = application.NewObject<Customer>();
await newCustomer.CombineLatest(listView, (customer, view) => {
    view.CollectionSource.Add(customer);
    return customer;
})
```

</twitter>

And so on, the sky is the limit here as you can write custom operators just like a common c# extension method that extends and returns an `IObservable<T>`. All modules of this repository that have a dependency to the Reactive package are developed similar to the above example. Explore their docs, tests, source to find endless examples.

--- 

**Possible future improvements:**

1. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

### Examples
All Xpand.XAF.Modules that reference this package are developed the RX way. Following the functional RX paradigm all modules do not use controllers rather describe the problem by composing events. 

Below we will add interesting examples. All methods can live in a static class.
##### Working with actions
1. First we register the action inside our module Setup method as registration needs an `ApplicationModulesManager`. The `RegisterViewSimpleAction` is an `Observable` which we need to `Subscribe` to it else it does nothing, same as IEnumerable does nothing if we do not enumerate. We pass the `this` argument to dispose subscription resources along with the module.
    ```cs
    public override void Setup(ApplicationModulesManager manager){
        base.Setup(manager);
        manager.RegisterViewSimpleAction(nameof(MyCustomAction)).Subscribe(this);
    }
    ```
1. Previously we used the `nameof(MyCustomAction)` to provide an action id, so now we will implement it like below:
    ```cs
    public static SimpleAction MyCustomAction(this (MyCustomModule, Frame frame) tuple) => tuple
        .frame.Action(nameof(MyCustomAction)).As<SimpleAction>();
    ```
    now our code consumers can use the next snippet at any place they have an Application dependency (Controller, Module, XafApplication, Program.cs, Global.asax.cs etc.):
    ```cs
    Application.WhenViewOnFrame(typeof(MyBO), ViewType.ListView, Nesting.Nested)
        .SelectMany(frame => frame.Action<MyCustomModule>().MyCustomAction().WhenExecute())
        .ExecuteTheAction()
        .Subscribe(this);
    ```

    For more examples consider the existing [tests](https://github.com/eXpandFramework/DevExpress.XAF/blob/master/src/Tests/Reactive/RegisterActionTests.cs).
3. Modify view artifacts (ViewItem, ListEditor, Business object, Nested Views) by implementing the `ExecuteTheAction` from previous step.
    ```cs
    private static IObservable<Unit> ExecuteTheAction(this IObservable<SimpleActionExecuteEventArgs> source) 
        => source.Do(e => {
                //get hold of the action
                var simpleAction = e.Action; 
                //get the frame for the frame.GetController()
                var frame = simpleAction.Controller.Frame;  
                var detailView = simpleAction.View<DetailView>();
                //get a propertyeditor
                var boNamePropertyEditor = detailView.GetPropertyEditor<MyCustomBO>(bo =>bo.Name ); 
                //get the listpropertyeditor
                var listPropertyEditor = detailView.GetListPropertyEditor<MyCustomBO>(bo => bo.Childs);
                //get the nestedlistView
                var nestedListView = listPropertyEditor.Frame.View; 
                
                //implement any master-child view scenario as all dependencies are known
            });

    ```
##### Working with Views
1. Modifying View artifacts similar to what was demoed with working with Actions:
    ```cs
    public override void Setup(ApplicationModulesManager moduleManager){
        base.Setup(moduleManager);
        moduleManager.WhenApplication().WhenListViewCreated()
            .When(typeof(MyCustomBo))
            .ControlsCreated()
            .Do(listview => {
                //filter the listview
                listview.CollectionSource.Criteria[""] = ...
            })
            .Subscribe(this);

    }
    ```
2. Utilizing the `Frame` useful to get for example traditional XAF Controllers.
    ```cs
    public override void Setup(ApplicationModulesManager moduleManager){
        base.Setup(moduleManager);
        moduleManager.WhenApplication().WhenViewOnFrame(typeof(MyCustomBo),ViewType.ListView)
            .Do(frame => {
                //get a xaf build in controller
                var listViewProcessCurrentObjectController = frame.GetController<ListViewProcessCurrentObjectController>();
            })
            .Subscribe(this);

    }

    ```
3. Working on Master-detail scenario: In previous step we saw how to wait for a view on a frame. In a master-detail scenario we need to wait for two views on a frame and then using the ReactiveX [Zip operator](http://reactivex.io/documentation/operators/zip.html) to pair them together.
    ```cs
    public override void Setup(ApplicationModulesManager moduleManager){
        base.Setup(moduleManager);
        //every time a MyCustom DetailView created together wuth a MyCustomBoChildListView
        moduleManager.WhenApplication().WhenViewOnFrame(typeof(MyCustomBo),ViewType.DetailView)
            .Zip(moduleManager.WhenApplication().WhenViewOnFrame(typeof(MyCustomBoChild),ViewType.ListView),
            //emit the created pair of frames
                (masterFrame, nestedFrame) => {
                    //set the parent object OrderCount to the count of child objects
                    ((MyCustomBo) masterFrame.View.CurrentObject).OrderCount = nestedFrame.View.AsListView()
                        .CollectionSource.Objects<MyCustomChildBo>().Count();
                    //return void
                    return Unit.Default;
                })
        
            .Subscribe(this);

    }

    ```

##### Working with NonPersistentObjects
1. Populate a ListView at any context (Popup, Navigation, Root, Nested)
    ```cs
    public override void Setup(ApplicationModulesManager moduleManager){
    base.Setup(moduleManager);
    moduleManager.WhenApplication().WhenListViewCreating(typeof(NonPersistentObject))
        .SelectMany(tuple => ((NonPersistentObjectSpace) tuple.args.ObjectSpace)
            .WhenObjectsGetting().Do(_ => {
                _.e.Objects = new BindingList<NonPersistentObject>();
            })
        )
        .Subscribe(this);
    }
    ```

##### Extending the XAF TypesInfo

1. Add a new member to an existing BO as:

    ```cs
    public override void Setup(ApplicationModulesManager moduleManager){
        base.Setup(moduleManager);
        moduleManager.WhenCustomizeTypesInfo()
            .Do(_ => (_.e.TypesInfo.FindTypeInfo(typeof(MyCustomBO))).CreateMember("MemberName",
                typeof(string)))
            .Subscribe(this);
    }

    ```

##### Extending the XAF Model

1. Extend the ListView model with the IModelListViewTest interface.

    ```cs
    public override void Setup(ApplicationModulesManager moduleManager){
        base.Setup(moduleManager);
        moduleManager.WhenExtendingModel()
            .Do(_ => _.Add<IModelListView,IModelListViewTest>())
            .Subscribe(this);
    }
    ```

2. Generate model nodes, for example add a new IModelFileImageSource.
    
    ```cs
    public override void Setup(ApplicationModulesManager moduleManager){
        base.Setup(moduleManager);
        moduleManager.WhenGeneratingModelNodes(modelApplication => modelApplication.ImageSources)
            .Do(_ => _.AddNode<IModelFileImageSource>())
            .Subscribe(this);
    }

    ```

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: net461`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|Fasterflect.Xpand|2.0.7
 |JetBrains.Annotations|2020.1.0
 |System.Interactive|4.1.1
 |System.Reactive|4.4.1
 |Xpand.Extensions|2.202.38
 |Xpand.Extensions.Reactive|2.202.39
 |Xpand.Extensions.XAF|2.202.39
 |Xpand.Patcher|2.0.23
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|2.202.9

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.Reactive.ReactiveModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Xpand.XAF.s.Reactive.Reactive). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)

