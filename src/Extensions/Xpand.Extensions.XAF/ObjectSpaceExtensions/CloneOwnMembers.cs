using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static TObject[] Map<TObject>(this IObjectSpace source,IObjectSpace target) where TObject:class,IObjectSpaceLink 
            => source.GetObjectsQuery<TObject>().ToArray()
                .Select(arg => {
                    var objectSpaceLink = target.CreateObject<TObject>();
                    arg.Map(objectSpaceLink);
                    return objectSpaceLink;
                }).ToArray()
                ;

        public static T Map<T>(this T source) where T : IObjectSpaceLink {
            var target = source.ObjectSpace.CreateObject<T>();
            source.Map(target);
            return target;
        }

        public static void Map(this IObjectSpace objectSpace, object source, object target)
            => source.GetTypeInfo().Members.Where(info => !info.IsReadOnly && !info.IsService)
                .Do(info => {
                    var value = info.GetValue(source);
                    if (info.MemberTypeInfo.IsPersistent) {
                        value = objectSpace.GetObject(value);
                    }

                    info.SetValue(target, value);

                }).Enumerate();
        
        public static void Map<T>(this T source, T target) where T:IObjectSpaceLink 
            => target.ObjectSpace.Map(source,target);
    }
}