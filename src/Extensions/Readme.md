# About
The  `Extensions` namespace is used for projects that contain **static** **internal** **extension** classes. 

There is no package or assembly though as the modules only link the methods they want to use. 

For example in the `Xpand.Source.Extensions.XAF.Model` namespace there is a `GetParent` method.

```cs
using DevExpress.ExpressApp.Model;

namespace Xpand.Source.Extensions.XAF.Model{
    internal static partial class Extensions{
        public static TNode GetParent<TNode>(this IModelNode modelNode) where TNode : class, IModelNode{
            if (modelNode is TNode node)
                return node;
            var parent = modelNode.Parent;
            while (!(parent is TNode)) {
                parent = parent.Parent;
                if (parent == null)
                    break;
            }
            return (TNode) parent;
        }

    }
}
```

The consumer modules link/compile this file only, minimizing the dependencies.
