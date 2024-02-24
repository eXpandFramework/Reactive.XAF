using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static TObject[] CloneObjects<TObject>(this IObjectSpace source,IObjectSpace target) where TObject:class,IObjectSpaceLink 
            => source.GetObjectsQuery<TObject>().ToArray()
                .Select(arg => {
                    var objectSpaceLink = target.CreateObject<TObject>();
                    arg.CloneObjects(objectSpaceLink);
                    return objectSpaceLink;
                }).ToArray()
                ;

        public static void CloneObjects<T>(this T source, T target) where T:IObjectSpaceLink 
            => source.GetTypeInfo().Members.Where(info => !info.IsReadOnly&&!info.IsService)
                .Do(info => {
                    var value = info.GetValue(source);
                    if (info.MemberTypeInfo.IsPersistent) {
                        value = target.ObjectSpace.GetObject(value);
                    }
                    info.SetValue(target, value);
                        
                }).Enumerate();
    }
}