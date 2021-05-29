using System.Reflection;

namespace ModellerLibDownloader
{
    public static class Connect
    {
        static Connect()
        {
            var version = Assembly.GetAssembly(typeof(DevExpress.Data.Entity.ConnectionStringInfo)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
            version = Assembly.GetAssembly(typeof(DevExpress.ExpressApp.Win.AlignmentHelper)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
            version = Assembly.GetAssembly(typeof(DevExpress.ExpressApp.Xpo.DataLockingHelper)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
            version = Assembly.GetAssembly(typeof(DevExpress.ExpressApp.Frame)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
            version = Assembly.GetAssembly(typeof(DevExpress.Office.AzureCompatibility)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
            version = Assembly.GetAssembly(typeof(DevExpress.Persistent.Base.Method)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
            version = Assembly.GetAssembly(typeof(DevExpress.Printing.Core.Native.IdGenerator)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
            version = Assembly.GetAssembly(typeof(DevExpress.RichEdit.Export.Settings)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
            version = Assembly.GetAssembly(typeof(DevExpress.Utils.Action0)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
            version = Assembly.GetAssembly(typeof(DevExpress.XtraTreeList.ScrollVisibility)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
            version = Assembly.GetAssembly(typeof(DevExpress.Xpo.Session)).GetCustomAttribute<AssemblyVersionAttribute>().Version;
        }
    }
}