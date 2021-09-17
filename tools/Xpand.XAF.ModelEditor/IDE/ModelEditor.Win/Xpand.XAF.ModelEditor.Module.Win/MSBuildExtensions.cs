using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Xpand.XAF.ModelEditor.Module.Win.BusinessObjects;

namespace Xpand.XAF.ModelEditor.Module.Win {
    internal static class MSBuildExtensions {
        static MSBuildExtensions() {
            try {
                MSBuildLocator.MSBuildLocator.RegisterDefaults();
            }
            catch {
                // ignored
            }
        }

        public static IEnumerable<(MSBuildProject msBuildProject,Project project)> Projects(this SolutionFile solution)
            => solution.ProjectsInOrder.Where(projectInSolution => projectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                .Select(projectInSolution => {
                    var project = new ProjectCollection(new Dictionary<string, string> {{"Configuration", solution.GetDefaultConfigurationName()}})
                        .LoadProject(projectInSolution.AbsolutePath);

                    var evaluatedProperties = project.AllEvaluatedProperties;
                    var msBuildProject = new MSBuildProject {
                        TargetFramework = evaluatedProperties.FirstOrDefault(property => property.Name == "TargetFramework"||property.Name == "TargetFrameworkVersion")?.EvaluatedValue,
                        Path = Path.GetFullPath(projectInSolution.AbsolutePath),
                        TargetFileName = evaluatedProperties.FirstOrDefault(property => property.Name == "TargetFileName")?.EvaluatedValue
                    };
                    if (string.Equals(evaluatedProperties
                        .FirstOrDefault(property => property.Name == "AppendTargetFrameworkToOutputPath")
                        ?.EvaluatedValue, true.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                        msBuildProject.AppendTargetFramework = true;
                    }
                    msBuildProject.OutputPath = project.EvaluateProperty("OutputPath");
                    var copyLocalLockFileAssemblies=$"{project.EvaluateProperty("CopyLocalLockFileAssemblies")}".ToLower();
                    if (copyLocalLockFileAssemblies == ""||copyLocalLockFileAssemblies=="false") {
                        msBuildProject.CannotRunMEMessage = "Please recompile, CopyLocalLockFileAssemblies was not set";
                        project.SetProperty("CopyLocalLockFileAssemblies", true.ToString());
                        project.Save(project.FullPath);
                    }
                    
                    if (!Path.IsPathRooted(msBuildProject.OutputPath)) {
                        msBuildProject.OutputPath = Path.GetFullPath($"{Path.GetDirectoryName(msBuildProject.Path)!}\\{msBuildProject.OutputPath}");
                    }
                    if (msBuildProject.AppendTargetFramework) {
                        msBuildProject.OutputPath += $"\\{msBuildProject.TargetFramework}";
                    }
                    msBuildProject.AssemblyPath = $"{msBuildProject.OutputPath}\\{msBuildProject.TargetFileName}";
                    var blazorStartup = project.Items.Any(item => item.EvaluatedInclude=="Properties\\launchSettings.json");
                    msBuildProject.IsApplicationProject =blazorStartup||$"{project.EvaluateProperty("OutputType")}".ToLower()=="winexe" ||msBuildProject.IsApplicationProject();
                    if ($"{msBuildProject.TargetFramework}".StartsWith("v")) {
                        msBuildProject.CannotRunMEMessage = "ME can be executed only for .NET5 and later version projects";
                    }
                    return (msBuildProject,project);
                });

        private static bool IsApplicationProject(this MSBuildProject msBuildProject) 
            => File.Exists($"{msBuildProject.Path}\\{msBuildProject.OutputPath}\\{Path.GetFileNameWithoutExtension(msBuildProject.TargetFileName)}.exe") || File.Exists(
                $"{Path.GetDirectoryName(msBuildProject.Path)}\\web.config")||File.Exists($"{Path.GetDirectoryName(msBuildProject.Path)}\\app.config");

        private static string EvaluateProperty(this Project item, string projectDir) 
            => item.AllEvaluatedProperties.FirstOrDefault(property => property.Name == projectDir)?.EvaluatedValue;

        public static bool AppendTargetFrameworkToOutputPath(this ProjectRootElement element)
            => element.Properties.FirstOrDefault(propertyElement => propertyElement.Name.Equals(nameof(AppendTargetFrameworkToOutputPath),
                StringComparison.OrdinalIgnoreCase))?.Value == "false";

        public static string OutPutPath(this ProjectRootElement element, MSBuildProject msBuildProject) {
            var path = element.Properties.FirstOrDefault(propertyElement => propertyElement.Name=="OutPutPath")?.Value;
            if (path == null) {
                path = $"{Path.GetDirectoryName(msBuildProject.Path)}\\bin\\debug";
                if (msBuildProject.AppendTargetFramework) {
                    path+= $"\\{msBuildProject.TargetFramework}";
                }
            }

            return !Path.IsPathRooted(path) ? Path.GetRelativePath(msBuildProject.Path, path) : path;
        }

    }
}