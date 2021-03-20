using System.Collections;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.CollectionSourceExtensions{
    public static partial class CollectionSourceExtensions{
        public static void AddRange(this CollectionSourceBase collectionSourceBase, IEnumerable objects){
			foreach (var o in objects){
				collectionSourceBase.Add(o);
			}
		}
	}
}