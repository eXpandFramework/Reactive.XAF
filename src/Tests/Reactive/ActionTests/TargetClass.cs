using System;
using HarmonyLib;
using NUnit.Framework;
using Shouldly;
using System.Reflection;

namespace Harmony10.Tests {
    public class TargetClass {
        public string GetValue() => "Original";
    }

    // MODIFICATION: START
    // Removed attributes to prevent auto-patching by the test runner or previous scans
    public static class ManualPatchClass {
        public static void Postfix(ref string __result) => __result = "ManualPatched";
    }
    // MODIFICATION: END

    public class ValidationTests {
        [Test]
        public void Assert_Harmony_Works_On_Net10_Manual() {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
                Console.WriteLine(e);
                return null;
            };
            var instance = new TargetClass();
            var harmony = new Harmony("com.validation.manual");
            var originalMethod = typeof(TargetClass).GetMethod(nameof(TargetClass.GetValue));
            var postfixMethod = typeof(ManualPatchClass).GetMethod(nameof(ManualPatchClass.Postfix));

            // Ensure we are clean - if this fails, the test host process MUST be restarted
            instance.GetValue().ShouldBe("Original");

            // Manually apply the patch - this triggers the ILGenerator logic that previously failed
            try {
                harmony.Patch(originalMethod, postfix: new HarmonyMethod(postfixMethod));
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }

            // Validate state change
            instance.GetValue().ShouldBe("ManualPatched");

            harmony.UnpatchAll("com.validation.manual");
            instance.GetValue().ShouldBe("Original");
        }
    }
}