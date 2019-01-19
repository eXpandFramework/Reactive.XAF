# About
The  `Extensions` namespace is used for projects that contain **static** **internal** **extension** classes. 

There is no package or assembly though as the modules only link the methods they want to use. 

For example in the `Xpand.Source.Extensions.XAF.Model` namespace there is a `GetParent` method.

https://github.com/eXpandFramework/XAF/blob/76e307ee628dba9df5296c00761e39cb1cb7c32d/src/Extensions/Extensions/XAF/Model/GetParent.cs#L5-L17

The consumer modules link/compile this file only, minimizing the dependencies.
