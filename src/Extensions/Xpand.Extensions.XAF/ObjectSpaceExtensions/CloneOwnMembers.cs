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
                    if (value!=null&&info.MemberTypeInfo.IsPersistent) {
                        value = objectSpace.GetObjectByKey(info.MemberType,objectSpace.GetKeyValue(value));
                    }

                    info.SetValue(target, value);

                }).Finally(() => {
                    var keyMember = source.GetTypeInfo().KeyMember;
                    if (!keyMember.IsReadOnly&&!keyMember.IsAutoGenerate) {
                        keyMember.SetValue(target,keyMember.GetValue(source));
                    }
                }).Enumerate();
        
        public static void Map<T>(this T source, T target) where T:IObjectSpaceLink 
            => target.ObjectSpace.Map(source,target);
    }
}