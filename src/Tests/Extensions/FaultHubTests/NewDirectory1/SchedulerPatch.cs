using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Relay;

namespace Xpand.Extensions.Tests.FaultHubTests.NewDirectory1;
[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class SchedulerPatch {
    private static bool _isInitialized;
    private static readonly Lock Lock = new();

    [HarmonyPatch("System.Reactive.Concurrency.SchedulerDefaults", "System.Reactive")]
    [HarmonyPatch("TimeBasedOperations", MethodType.Getter)]
    internal static class SchedulerDefaults_TimeBasedOperations_Patch {
        [HarmonyPostfix]
        internal static void Postfix(ref IScheduler __result) {

            LogFast($"HarmonyPatch: Postfix for TimeBasedOperations. Replacing result with ContextAwareDefaultScheduler.Instance.");
            __result = new XpandScheduler(__result);
        }
    }
    [HarmonyPatch("System.Reactive.Concurrency.SchedulerDefaults", "System.Reactive")]
    [HarmonyPatch("ConstantTimeOperations", MethodType.Getter)]
    internal static class SchedulerDefaults_ConstantTimeOperations_Patch {
        [HarmonyPostfix]
        internal static void Postfix(ref IScheduler __result) {

            LogFast($"HarmonyPatch: Postfix for ConstantTimeOperations. Replacing result with ContextAwareDefaultScheduler.Instance.");
            __result = new XpandScheduler(__result);
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DefaultScheduler), nameof(DefaultScheduler.Instance), MethodType.Getter)]
    internal static void DefaultScheduler_Instance_Postfix(ref IScheduler __result) {
        if (__result is XpandScheduler) return;
        LogFast($"HarmonyPatch: Postfix for Scheduler.Default. Wrapping result.");
        __result = new XpandScheduler(__result);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NewThreadScheduler), nameof(NewThreadScheduler.Default), MethodType.Getter)]
    internal static void NewThreadScheduler_Default_Postfix(ref IScheduler __result) {
        if (__result is XpandScheduler) return;
        LogFast($"HarmonyPatch: Postfix for Scheduler.Default. Wrapping result.");
        __result = new XpandScheduler(__result);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TaskPoolScheduler), nameof(TaskPoolScheduler.Default), MethodType.Getter)]
    internal static void TaskPoolScheduler_Default_Postfix(ref IScheduler __result) {
        if (__result is XpandScheduler) return;
        LogFast($"HarmonyPatch: Postfix for Scheduler.Default. Wrapping result.");
        __result = new XpandScheduler(__result);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThreadPoolScheduler), nameof(ThreadPoolScheduler.Instance), MethodType.Getter)]
    internal static void ThreadPoolScheduler_Instance_Postfix(ref IScheduler __result) {
        if (__result is XpandScheduler) return;
        LogFast($"HarmonyPatch: Postfix for Scheduler.Default. Wrapping result.");
        __result = new XpandScheduler(__result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ImmediateScheduler), nameof(ImmediateScheduler.Instance), MethodType.Getter)]
    internal static void ImmediateScheduler_Instance_Postfix(ref IScheduler __result) {
        if (__result is XpandScheduler) return;
        LogFast($"HarmonyPatch: Postfix for ImmediateScheduler.Instance. Wrapping result.");
        __result = new XpandScheduler(__result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CurrentThreadScheduler), nameof(CurrentThreadScheduler.Instance), MethodType.Getter)]
    internal static void CurrentThreadScheduler_Instance_Postfix(ref IScheduler __result) {
        if (__result is XpandScheduler) return;
        LogFast($"HarmonyPatch: Postfix for CurrentThreadScheduler.Instance. Wrapping result.");
        __result = new XpandScheduler(__result);
    }
    

    public static void Initialize() {
        lock (Lock) {
            if (_isInitialized) return;
            var harmony = new HarmonyLib.Harmony("Xpand.Extensions.Tests.FaultHub.SchedulerDefaults.Patch");
            harmony.PatchAll(typeof(SchedulerPatch).Assembly);
            // harmony.PatchAll(typeof(QueryLanguagePatch).Assembly);
            
            FaultHub.Reset();
            AsyncLocalFactory.RegisteredContexts.Execute(local => XpandScheduler.Contexts.Add(local)).Enumerate();
            _isInitialized = true;
        }
    }
}