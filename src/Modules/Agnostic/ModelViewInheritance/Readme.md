# About

The `ModuleViewInheritance` allows to replace the generator layer of a view by composing other view model differences.
## Installation 
1. `Install-Package DevExpress.XAF.Modules.ModelViewInheritance`
2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
## Details
The module extends the model views nodes with the `IModelObjectViewMergedDifferences` interface to allow model view differences linking. 
### Tests
The module is testsed on each build from the [available tests](https://github.com/eXpandFramework/Packages/tree/master/src/Specifications/Modules/ModelViewInheritance)

### Examples
The module already integrates with `eXpandFramework` as a shared functionality and is installed if you use and of its mmodules, so there is no need for explicit registration.

Bellow are a few examples of how we use the module in `eXpandFramework`. 

The `XpandSecurityModule` defines a custom view for 

