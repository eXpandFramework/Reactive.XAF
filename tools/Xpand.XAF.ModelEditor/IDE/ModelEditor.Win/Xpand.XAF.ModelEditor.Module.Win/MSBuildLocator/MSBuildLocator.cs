﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Xpand.XAF.ModelEditor.Module.Win.MSBuildLocator {
    public static class MSBuildLocator {
        private const string MSBuildPublicKeyToken = "b03f5f7f11d50a3a";

        private static readonly string[] SmsBuildAssemblies = {
            "Microsoft.Build",
            "Microsoft.Build.Engine",
            "Microsoft.Build.Framework",
            "Microsoft.Build.Tasks.Core",
            "Microsoft.Build.Utilities.Core",
        };

#if NET46
        private static ResolveEventHandler s_registeredHandler;
#else
        private static Func<AssemblyLoadContext, AssemblyName, Assembly> _sRegisteredHandler;
#endif

        /// <summary>
        ///     Gets a value indicating whether an instance of MSBuild is currently registered.
        /// </summary>
        public static bool IsRegistered => _sRegisteredHandler != null;

        /// <summary>
        ///     Gets a value indicating whether an instance of MSBuild can be registered.
        /// </summary>
        /// <remarks>
        ///     If any Microsoft.Build assemblies are already loaded into the current AppDomain, the value will be false.
        /// </remarks>
        public static bool CanRegister => !IsRegistered && !LoadedMsBuildAssemblies.Any();

        private static IEnumerable<Assembly> LoadedMsBuildAssemblies =>
            AppDomain.CurrentDomain.GetAssemblies().Where(IsMSBuildAssembly);

        /// <summary>
        ///     Query for all Visual Studio instances.
        /// </summary>
        /// <remarks>
        ///     Only includes Visual Studio 2017 (v15.0) and higher.
        /// </remarks>
        /// <returns>Enumeration of all Visual Studio instances detected on the machine.</returns>
        public static IEnumerable<VisualStudioInstance> QueryVisualStudioInstances() 
            => QueryVisualStudioInstances(VisualStudioInstanceQueryOptions.Default);

        /// <summary>
        ///     Query for Visual Studio instances matching the given options.
        /// </summary>
        /// <remarks>
        ///     Only includes Visual Studio 2017 (v15.0) and higher.
        /// </remarks>
        /// <param name="options">Query options for Visual Studio instances.</param>
        /// <returns>Enumeration of Visual Studio instances detected on the machine.</returns>
        public static IEnumerable<VisualStudioInstance> QueryVisualStudioInstances(
            VisualStudioInstanceQueryOptions options) 
            => QueryVisualStudioInstances(GetInstances(options), options);

        internal static IEnumerable<VisualStudioInstance> QueryVisualStudioInstances(
            IEnumerable<VisualStudioInstance> instances,
            VisualStudioInstanceQueryOptions options) {
            return instances.Where(i => options.DiscoveryTypes.HasFlag(i.DiscoveryType));
        }

        /// <summary>
        ///     Discover instances of Visual Studio and register the first one. See <see cref="RegisterInstance" />.
        /// </summary>
        /// <returns>Instance of Visual Studio found and registered.</returns>
        public static VisualStudioInstance RegisterDefaults() {
            VisualStudioInstance instance = GetInstances(VisualStudioInstanceQueryOptions.Default).FirstOrDefault();
            if (instance == null) {
                var error = "No instances of MSBuild could be detected." +
                            Environment.NewLine +
                            $"Try calling {nameof(RegisterInstance)} or {nameof(RegisterMSBuildPath)} to manually register one.";

                throw new InvalidOperationException(error);
            }

            RegisterInstance(instance);

            return instance;
        }

        /// <summary>
        ///     Add assembly resolution for Microsoft.Build core dll in the current AppDomain from the specified
        ///     instance of Visual Studio. See <see cref="QueryVisualStudioInstances()" /> to discover Visual Studio
        ///     instances or use <see cref="RegisterDefaults" />.
        /// </summary>
        /// <param name="instance"></param>
        public static void RegisterInstance(VisualStudioInstance instance) {
            if (instance == null) {
                throw new ArgumentNullException(nameof(instance));
            }

            if (instance.DiscoveryType == DiscoveryType.DotNetSdk) {
                // The dotnet cli sets up these environment variables when msbuild is invoked via `dotnet`,
                // but we are using msbuild dlls directly and therefore need to mimic that.
                ApplyDotNetSdkEnvironmentVariables(instance.MSBuildPath);
            }

            // Find and load NuGet assemblies if msbuildPath is in a VS installation
            string nugetPath = Path.GetFullPath(Path.Combine(instance.MSBuildPath, "..", "..", "..", "Common7", "IDE",
                "CommonExtensions", "Microsoft", "NuGet"));
            if (Directory.Exists(nugetPath)) {
                RegisterMSBuildPath(new[] {instance.MSBuildPath, nugetPath});
            }
            else {
                RegisterMSBuildPath(instance.MSBuildPath);
            }
        }

        /// <summary>
        ///     Add assembly resolution for Microsoft.Build core dlls in the current AppDomain from the specified
        ///     path.
        /// </summary>
        /// <param name="msbuildPath">
        ///     Path to the directory containing a deployment of MSBuild binaries.
        ///     A minimal MSBuild deployment would be the publish result of the Microsoft.Build.Runtime package.
        ///
        ///     In order to restore and build real projects, one needs a deployment that contains the rest of the toolchain (nuget, compilers, etc.).
        ///     Such deployments can be found in installations such as Visual Studio or dotnet CLI.
        /// </param>
        public static void RegisterMSBuildPath(string msbuildPath) {
            RegisterMSBuildPath(new[] {msbuildPath});
        }

        class CannotRegisterException:InvalidOperationException {
            public CannotRegisterException(string message) : base(message) {
            }
        }
        /// <summary>
        ///     Add assembly resolution for Microsoft.Build core dlls in the current AppDomain from the specified
        ///     path.
        /// </summary>
        /// <param name="msbuildSearchPaths">
        ///     Paths to directories containing a deployment of MSBuild binaries.
        ///     A minimal MSBuild deployment would be the publish result of the Microsoft.Build.Runtime package.
        ///
        ///     In order to restore and build real projects, one needs a deployment that contains the rest of the toolchain (nuget, compilers, etc.).
        ///     Such deployments can be found in installations such as Visual Studio or dotnet CLI.
        /// </param>
        public static void RegisterMSBuildPath(string[] msbuildSearchPaths) {
            if (msbuildSearchPaths.Length < 1) {
                throw new ArgumentException("Must provide at least one search path to RegisterMSBuildPath.");
            }

            List<ArgumentException> nullOrWhiteSpaceExceptions = new List<ArgumentException>();
            for (int i = 0; i < msbuildSearchPaths.Length; i++) {
                if (string.IsNullOrWhiteSpace(msbuildSearchPaths[i])) {
                    nullOrWhiteSpaceExceptions.Add(new ArgumentException(
                        $"Value at position {i + 1} may not be null or whitespace", nameof(msbuildSearchPaths)));
                }
            }

            if (nullOrWhiteSpaceExceptions.Count > 0) {
                throw new AggregateException(
                    "Search paths for MSBuild assemblies cannot be null and must contain non-whitespace characters.",
                    nullOrWhiteSpaceExceptions);
            }

            IEnumerable<string> paths = msbuildSearchPaths.Where(path => !Directory.Exists(path)).ToArray();
            if (paths.Any()) {
                throw new AggregateException(
                    $"A directory or directories in \"{nameof(msbuildSearchPaths)}\" do not exist",
                    paths.Select(path =>
                        new ArgumentException($"Directory \"{path}\" does not exist", nameof(msbuildSearchPaths))));
            }

            if (!CanRegister) {
                var loadedAssemblyList =
                    string.Join(Environment.NewLine, LoadedMsBuildAssemblies.Select(a => a.GetName()));

                var error =
                    $"{typeof(MSBuildLocator)}.{nameof(RegisterInstance)} was called, but MSBuild assemblies were already loaded." +
                    Environment.NewLine +
                    $"Ensure that {nameof(RegisterInstance)} is called before any method that directly references types in the Microsoft.Build namespace has been called." +
                    Environment.NewLine +
                    "This dependency arises from when a method is just-in-time compiled, so if it breaks even if the reference to a Microsoft.Build type has not been executed." +
                    Environment.NewLine +
                    "For more details, see aka.ms/RegisterMSBuildLocator" +
                    Environment.NewLine +
                    "Loaded MSBuild assemblies: " +
                    loadedAssemblyList;

                throw new CannotRegisterException(error);
            }
            
            // AssemblyResolve event can fire multiple times for the same assembly, so keep track of what's already been loaded.
            var loadedAssemblies = new Dictionary<string, Assembly>();

            // Saving the handler in a static field so it can be unregistered later.
#if NET46
            s_registeredHandler = (_, eventArgs) =>
            {
                var assemblyName = new AssemblyName(eventArgs.Name);
                return TryLoadAssembly(new AssemblyName(eventArgs.Name));
            };

            AppDomain.CurrentDomain.AssemblyResolve += s_registeredHandler;
#else
            _sRegisteredHandler = (_, assemblyName) => TryLoadAssembly(assemblyName);

            AssemblyLoadContext.Default.Resolving += _sRegisteredHandler;
#endif

            return;

            Assembly TryLoadAssembly(AssemblyName assemblyName) {
                // Assembly resolution is not thread-safe.
                lock (loadedAssemblies) {
                    if (loadedAssemblies.TryGetValue(assemblyName.FullName, out Assembly assembly)) {
                        return assembly;
                    }

                    // Look in the MSBuild folder for any unresolved reference. It may be a dependency
                    // of MSBuild or a task.
                    foreach (string msbuildPath in msbuildSearchPaths) {
                        string targetAssembly = Path.Combine(msbuildPath, assemblyName.Name + ".dll");
                        if (File.Exists(targetAssembly)) {
                            assembly = Assembly.LoadFrom(targetAssembly);
                            loadedAssemblies.Add(assemblyName.FullName, assembly);
                            return assembly;
                        }
                    }

                    return null;
                }
            }
        }

        /// <summary>
        ///     Remove assembly resolution previously registered via <see cref="RegisterInstance" />, <see cref="RegisterMSBuildPath" />, or <see cref="RegisterDefaults" />.
        /// </summary>
        /// <remarks>
        ///     This will automatically be called once all supported assemblies are loaded into the current AppDomain and so generally is not necessary to call directly.
        /// </remarks>
        [SuppressMessage("ReSharper", "InvalidXmlDocComment")]
        public static void Unregister() {
            if (!IsRegistered) {
                var error =
                    $"{typeof(MSBuildLocator)}.{nameof(Unregister)} was called, but no MSBuild instance is registered." +
                    Environment.NewLine +
                    $"Ensure that {nameof(RegisterInstance)}, {nameof(RegisterMSBuildPath)}, or {nameof(RegisterDefaults)} is called before calling this method." +
                    Environment.NewLine +
                    $"{nameof(IsRegistered)} should be used to determine whether calling {nameof(Unregister)} is a valid operation.";
                throw new InvalidOperationException(error);
            }

#if NET46
            AppDomain.CurrentDomain.AssemblyResolve -= s_registeredHandler;
#else
            AssemblyLoadContext.Default.Resolving -= _sRegisteredHandler;
#endif
        }

        /// <summary>
        ///     Ensures the proper MSBuild environment variables are populated for DotNet SDK.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void ApplyDotNetSdkEnvironmentVariables(string dotNetSdkPath) {
            const string MSBUILD_EXE_PATH = nameof(MSBUILD_EXE_PATH);
            const string MSBuildExtensionsPath = nameof(MSBuildExtensionsPath);
            const string MSBuildSDKsPath = nameof(MSBuildSDKsPath);

            var variables = new Dictionary<string, string> {
                [MSBUILD_EXE_PATH] = dotNetSdkPath + "MSBuild.dll",
                [MSBuildExtensionsPath] = dotNetSdkPath,
                [MSBuildSDKsPath] = dotNetSdkPath + "Sdks"
            };

            foreach (var kvp in variables) {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }

        private static bool IsMSBuildAssembly(Assembly assembly) => IsMSBuildAssembly(assembly.GetName());

        private static bool IsMSBuildAssembly(AssemblyName assemblyName) {
            if (!SmsBuildAssemblies.Contains(assemblyName.Name, StringComparer.OrdinalIgnoreCase)) {
                return false;
            }

            var publicKeyToken = assemblyName.GetPublicKeyToken();
            if (publicKeyToken == null || publicKeyToken.Length == 0) {
                return false;
            }

            var sb = new StringBuilder();
            foreach (var b in publicKeyToken) {
                sb.Append($"{b:x2}");
            }

            return sb.ToString().Equals(MSBuildPublicKeyToken, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<VisualStudioInstance> GetInstances(VisualStudioInstanceQueryOptions options) {
#if NET46
            var devConsole = GetDevConsoleInstance();
            if (devConsole != null)
                yield return devConsole;

    #if FEATURE_VISUALSTUDIOSETUP
            foreach (var instance in VisualStudioLocationHelper.GetInstances())
                yield return instance;
    #endif
#endif

#if NETCOREAPP
            foreach (var dotnetSdk in DotNetSdkLocationHelper.GetInstances(options.WorkingDirectory))
                yield return dotnetSdk;
#endif
        }

#if NET46
        private static VisualStudioInstance GetDevConsoleInstance()
        {
            var path = Environment.GetEnvironmentVariable("VSINSTALLDIR");
            if (!string.IsNullOrEmpty(path))
            {
                var versionString = Environment.GetEnvironmentVariable("VSCMD_VER");
                Version version;
                Version.TryParse(versionString, out version);

                if (version == null && versionString?.Contains('-') == true)
                {
                    versionString = versionString.Substring(0, versionString.IndexOf('-'));
                    Version.TryParse(versionString, out version);
                }

                if (version == null)
                {
                    versionString = Environment.GetEnvironmentVariable("VisualStudioVersion");
                    Version.TryParse(versionString, out version);
                }

                if(version != null)
                {
                    return new VisualStudioInstance("DEVCONSOLE", path, version, DiscoveryType.DeveloperConsole);
                }
            }

            return null;
        }
#endif
    }
}