using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static void Load<TBuilder>(this IModuleBuilder<TBuilder> builder, string path)
            where TBuilder : IXafApplicationBuilder<TBuilder> {
            if (!Directory.Exists(path)) return;
            Directory.GetFiles(path, "*.dll")
                .SelectMany(file => Assembly.LoadFile(file).GetTypes().Where(type => typeof(ModuleBase).IsAssignableFrom(type)))
                .Do(type => builder.Add(() => (ModuleBase)type.CreateInstance()))
                .Enumerate();
        }
    }
}