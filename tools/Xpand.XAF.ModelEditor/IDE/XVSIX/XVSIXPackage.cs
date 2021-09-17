using Microsoft.VisualStudio.Shell;
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using Fasterflect;
using Microsoft.VisualStudio.ExtensionManager;
using Task = System.Threading.Tasks.Task;

namespace XVSIX {
    
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(XVSIXPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class XVSIXPackage : AsyncPackage {
        
        public const string PackageGuidString = "d4fcb05c-7ed0-409e-a0e6-9d4174a7eb03";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await ModelEditorCommand.InitializeAsync(this);
        }

        public IObservable<Version> Version() 
            => GetServiceAsync(typeof(SVsExtensionManager)).ToObservable()
                .Select(manager => (Version) manager.CallMethod("GetInstalledExtension",
                    "XVSIX.2949c0e7-0388-4388-93b0-8aab7d13e4a1").GetPropertyValue("Header").GetPropertyValue("Version")).Cast<Version>();
    }
}