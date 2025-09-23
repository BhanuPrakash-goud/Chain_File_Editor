using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChainFileEditor.Core.Constants;

namespace ChainFileEditor.Core.Operations
{
    public sealed class FeatureChainService
    {
        private const string DefaultMode = "source";
        private const string BinaryMode = "binary";
        private const string IgnoreMode = "ignore";
        private const string TrueValue = "true";
        private const string FalseValue = "false";
        private const string DevBranchPrefix = "dev/dev-DEPM-";
        private const string CommentPrefix = "#";
        private const string FileExtension = ".properties";
        private const string SpaceReplacement = "-";
        
        private static readonly string[] ProjectOrder = { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment", "tests" };
        private static readonly string[] NoTestsProjects = { "content", "deployment", "tests", "designer" };
        private static readonly string[] IgnoreDevModeProjects = { "designer", "deployment", "tests" };
        private static readonly string[] IntegrationTests = {
            "AdhocWidgetSet1", "AdhocWidgetSet2", "AdministrationService", "AppEngineService",
            "AppsProvisioning", "AppStudioService", "BusinessModelingServiceSet1", "BusinessModelingServiceSet2",
            "BusinessModelingServiceSet3", "ConsolidationService", "DashboardsService", "dEPMAppsUpdate",
            "FarmCreation", "FarmUpgrade", "OfficeIntegrationService", "OlapService", "OlapAPI",
            "ContentIntegration", "dEPMRegressionSet1", "dEPMRegressionSet2", "dEPMRegressionSet3",
            "dEPMRegressionSet4", "SelfService", "WorkforceBudgetingSet1", "WorkforceBudgetingSet2",
            "WorkforceBudgetingSet4", "WorkforceBudgetingSet5", "MultiFarm", "EPMWorkflow",
            "ModelingService", "ModelingUI", "RelationalModeling", "FinancialReportingSet1", "FinancialReportingSet2"
        };
        public class ProjectConfig
        {
            public string ProjectName { get; set; } = string.Empty;
            public string Mode { get; set; } = DefaultMode; // source, binary, ignore
            public string DevMode { get; set; } = string.Empty; // Optional: binary, ignore, or empty
            public string Branch { get; set; } = string.Empty;
            public string Tag { get; set; } = string.Empty;
            public bool TestsEnabled { get; set; } = true;
            public string ForkRepository { get; set; } = string.Empty;
        }

        public class FeatureChainRequest
        {
            public string JiraId { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty; // Integration build number for global.version.binary
            public string DevsVersion { get; set; } = string.Empty; // Feature build number for global.devs.version.binary
            public string Recipients { get; set; } = string.Empty; // Email recipients
            public string IntegrationTag { get; set; } = string.Empty; // Integration tag for downstream projects
            public List<ProjectConfig> Projects { get; set; } = new List<ProjectConfig>();
            public List<string> EnabledIntegrationTests { get; set; } = new List<string>();
        }

        public string CreateFeatureChainFile(FeatureChainRequest request, string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(request.JiraId))
                throw new ArgumentException("JIRA ID is required");

            if (string.IsNullOrWhiteSpace(request.Description))
                throw new ArgumentException("Description is required");

            var fileName = $"dev-DEPM-{request.JiraId}-{SanitizeFileName(request.Description)}{FileExtension}";
            var filePath = Path.Combine(outputDirectory, fileName);

            var content = new List<string>();
            
            // Add header comment
            content.Add(HeaderComments.FeatureChainFile);
            content.Add(HeaderComments.Empty);
            content.Add($"{HeaderComments.JiraPrefix}{request.JiraId}");
            content.Add($"{HeaderComments.DescriptionPrefix}{request.Description}");
            content.Add(string.Empty);
            
            // Add global properties - both versions should be the same
            if (!string.IsNullOrWhiteSpace(request.Version))
            {
                content.Add($"{GlobalPropertyNames.VersionBinary}={request.Version}");
                content.Add($"{GlobalPropertyNames.DevVersionBinary}={request.Version}");
            }
            if (!string.IsNullOrWhiteSpace(request.Recipients))
            {
                content.Add($"{GlobalPropertyNames.Recipients}={request.Recipients}");
            }
            content.Add(string.Empty);

            // Project order as documented in build process
            foreach (var projectName in ProjectOrder)
            {
                var project = request.Projects.FirstOrDefault(p => p.ProjectName == projectName);
                
                if (project != null)
                {
                    // Project has feature branch - use source mode
                    content.Add($"{projectName}.{PropertyNames.Mode}={DefaultMode}");
                    
                    // Add mode.devs (commented if not selected, uncommented if selected)
                    if (!string.IsNullOrEmpty(project.DevMode))
                        content.Add($"{projectName}.{PropertyNames.DevMode}={project.DevMode}");
                    else
                        content.Add($"{CommentPrefix}{projectName}.{PropertyNames.DevMode}={BinaryMode}");
                    
                    // Add fork if specified
                    if (!string.IsNullOrWhiteSpace(project.ForkRepository))
                        content.Add($"{projectName}.{PropertyNames.Fork}={project.ForkRepository}");
                    
                    // Add branch or tag (never both)
                    if (!string.IsNullOrWhiteSpace(project.Branch))
                    {
                        var branchName = project.Branch;
                        if (branchName == BranchNames.Dev)
                        {
                            branchName = $"{DevBranchPrefix}{request.JiraId}-{SanitizeFileName(request.Description)}";
                        }
                        content.Add($"{projectName}.{PropertyNames.Branch}={branchName}");
                    }
                    else if (!string.IsNullOrWhiteSpace(project.Tag))
                        content.Add($"{projectName}.{PropertyNames.Tag}={project.Tag}");
                    
                    // Add tests.unit
                    if (project.TestsEnabled && !NoTestsProjects.Contains(projectName))
                        content.Add($"{projectName}.{PropertyNames.TestsUnit}={TrueValue}");
                    else if (NoTestsProjects.Contains(projectName))
                        content.Add($"{projectName}.{PropertyNames.TestsUnit}={FalseValue}");
                }
                else
                {
                    // Project without feature branch
                    var isUpstream = IsUpstreamProject(projectName, request.Projects, ProjectOrder);
                    
                    if (isUpstream)
                    {
                        // Upstream projects use binary mode
                        content.Add($"{projectName}.{PropertyNames.Mode}={BinaryMode}");
                    }
                    else
                    {
                        // Downstream projects use source mode with tag
                        content.Add($"{projectName}.{PropertyNames.Mode}={DefaultMode}");
                        
                        // Add mode.devs
                        if (IgnoreDevModeProjects.Contains(projectName))
                            content.Add($"{CommentPrefix}{projectName}.{PropertyNames.DevMode}={IgnoreMode}");
                        else
                            content.Add($"{CommentPrefix}{projectName}.{PropertyNames.DevMode}={BinaryMode}");
                        
                        // Add tag with integration build
                        if (!string.IsNullOrWhiteSpace(request.Version))
                            content.Add($"{CommentPrefix}{projectName}.{PropertyNames.Tag}=Build_12.25.11.{request.Version}");
                        else
                            content.Add($"{CommentPrefix}{projectName}.{PropertyNames.Branch}={BranchNames.Integration}");
                        
                        // Add tests.unit
                        if (NoTestsProjects.Contains(projectName))
                            content.Add($"{projectName}.{PropertyNames.TestsUnit}={FalseValue}");
                        else
                            content.Add($"{projectName}.{PropertyNames.TestsUnit}={TrueValue}");
                    }
                }
                
                content.Add(string.Empty);
            }

            // Add integration tests section
            content.Add(string.Empty);
            
            foreach (var test in IntegrationTests)
            {
                if (request.EnabledIntegrationTests.Contains(test))
                {
                    content.Add($"tests.{test}.run={TrueValue}");
                }
                else
                {
                    content.Add($"{CommentPrefix}tests.{test}.run={TrueValue}");
                }
            }

            // Remove last empty line
            if (content.Count > 0 && content.Last() == string.Empty)
                content.RemoveAt(content.Count - 1);

            File.WriteAllLines(filePath, content);
            return filePath;
        }

        public List<string> GetAvailableProjects()
        {
            return ProjectOrder.ToList();
        }

        public static List<string> GetBranchesForProject(string projectName)
        {
            return new List<string> { BranchNames.Main, BranchNames.Develop, BranchNames.Stage, BranchNames.Integration };
        }

        public static List<string> GetForksForProject(string projectName)
        {
            return new List<string> { $"user.name/{projectName}", $"team.name/{projectName}" };
        }

        public static List<string> GetBranchesForFork(string forkName)
        {
            return new List<string> { BranchNames.Main, BranchNames.Develop, "feature/branch-name" };
        }

        public static List<string> GetAvailableModes()
        {
            return new List<string> { DefaultMode, BinaryMode, IgnoreMode };
        }


        


        private static bool IsUpstreamProject(string projectName, List<ProjectConfig> featureProjects, string[] projectOrder)
        {
            var firstFeatureProjectIndex = projectOrder.Length;
            
            foreach (var featureProject in featureProjects)
            {
                var index = Array.IndexOf(projectOrder, featureProject.ProjectName);
                if (index >= 0 && index < firstFeatureProjectIndex)
                {
                    firstFeatureProjectIndex = index;
                }
            }
            
            var currentProjectIndex = Array.IndexOf(projectOrder, projectName);
            return currentProjectIndex < firstFeatureProjectIndex;
        }

        private static string SanitizeFileName(string description)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join(string.Empty, description.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Replace(" ", SpaceReplacement).ToLower();
        }

        public static bool IsVersionInValidRange(string version)
        {
            if (int.TryParse(version, out var versionNumber))
            {
                var config = ConfigurationLoader.LoadValidationConfig();
                var minVersion = config.ValidationSettings.VersionRange.MinVersion;
                var maxVersion = config.ValidationSettings.VersionRange.MaxVersion;
                return versionNumber >= minVersion && versionNumber <= maxVersion;
            }
            return false;
        }

        public static string GetVersionRangeWarningMessage(string version)
        {
            var config = ConfigurationLoader.LoadValidationConfig();
            var minVersion = config.ValidationSettings.VersionRange.MinVersion;
            var maxVersion = config.ValidationSettings.VersionRange.MaxVersion;
            return $"Warning: Version {version} is outside the recommended range ({minVersion}-{maxVersion}). Do you want to continue?";
        }
    }
    

}