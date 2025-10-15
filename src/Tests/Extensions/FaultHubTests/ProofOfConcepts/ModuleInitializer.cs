using System.Runtime.CompilerServices;

namespace Xpand.Extensions.Tests.FaultHubTests.ProofOfConcepts{
    public class ModuleInitializer {
        [ModuleInitializer]
        public static void Initialize() {
            // SchedulerPatch.Initialize();
        }

    }
}