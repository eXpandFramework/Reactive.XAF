using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;



// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Xpand.Source.Extensions")]
[assembly: AssemblyDescription("Xpand.Source.Extensions")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("DevExpress.XAF.Extensions")]
[assembly: AssemblyCopyright("Copyright ©  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("90e34ae8-5130-4087-9720-b9980c1f22f1")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:

[assembly: AssemblyVersion("1.0.11.0")]
[assembly: AssemblyFileVersion("1.0.11.0")]
//[assembly:AllowPartiallyTrustedCallers]
//[assembly: SecurityTransparent()]
[assembly:InternalsVisibleTo(XpandInfo.Tests+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.TestsLib+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.AutoCommit+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.Reactive+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.RefreshView+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.CloneMemberValue+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.RefreshView+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.CloneModelView+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.ReactiveLogger+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.OneView+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.GridListEditor+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.ReactiveLoggerHub+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.HideToolBar+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.MasterDetail+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.ModelMapper+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.ModelViewInheritance+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.ProgressBarViewItem+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.SuppressConfirmation+ ",PublicKey="+XpandInfo.Key)]
[assembly:InternalsVisibleTo(XpandInfo.ViewEditMode+ ",PublicKey="+XpandInfo.Key)]

// ReSharper disable once CheckNamespace
public class XpandInfo{
    public const string Tests = "Tests";
    public const string TestsLib = "TestsLib";
    public const string AutoCommit = "Xpand.XAF.Modules.AutoCommit.Tests";
    public const string Reactive = "Xpand.XAF.Modules.Reactive.Tests";
    public const string ReactiveLogger = "Xpand.XAF.Modules.Reactive.Logger.Tests";
    public const string OneView = "Xpand.XAF.Modules.OneView.Tests";
    public const string GridListEditor = "Xpand.XAF.Modules.GridListEditor.Tests";
    public const string ReactiveLoggerHub = "Xpand.XAF.Modules.Reactive.Logger.Hub.Tests";
    public const string CloneModelView = "Xpand.XAF.Modules.CloneModelView.Tests";
    public const string RefreshView = "Xpand.XAF.Modules.RefreshView.Tests";
    public const string CloneMemberValue = "Xpand.XAF.Modules.CloneMemberValue.Tests";
    public const string HideToolBar = "Xpand.XAF.Modules.HideToolBar.Tests";
    public const string MasterDetail = "Xpand.XAF.Modules.MasterDetail.Tests";
    public const string ModelMapper = "Xpand.XAF.Modules.ModelMapper.Tests";
    public const string ModelViewInheritance = "Xpand.XAF.Modules.ModelViewInheritance.Tests";
    public const string ProgressBarViewItem = "Xpand.XAF.Modules.ProgressBarViewItem.Tests";
    public const string SuppressConfirmation = "Xpand.XAF.Modules.SuppressConfirmation.Tests";
    public const string ViewEditMode = "Xpand.XAF.Modules.ViewEditMode.Tests";
    public const string Key =
        "0024000004800000940000000602000000240000525341310004000001000100df18f4f3de9ec490707183c78a72914070a526bfb1818e1687442b137c2bfa9bf5e8533859a8efaa62aa2ea28e03623fef5531f8dd29d74f781a9e50743172dbe8d74b0106ceddfcda17f8dd1034f2896a56e1026faa2cc0e2def8dc1f519ad13924c44f16339a57ed97981a8777c7fa6025a11e54cc694e504d462a400681c0";
}