using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Xpand.Extensions.Tests.FaultHubTests.NewDirectory1{
    internal static class ModuleInitializer {
        [ModuleInitializer]
        internal static void Initialize() {
            Enabled = true;
            // Write = s => {
            //     Console.WriteLine(s);
            //     Debug.WriteLine(s);
            //     TestContext.WriteLine(s);
            // };
            // SchedulerPatch.Initialize();
        }
    }
}