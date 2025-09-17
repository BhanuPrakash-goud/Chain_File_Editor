using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChainFileEditor.Core.Operations
{
    public class FeatureChainService
    {
        public class ProjectConfig
        {
            public string ProjectName { get; set; } = string.Empty;
            public string Mode { get; set; } = "source"; // source, binary, ignore
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

            var fileName = $"dev-DEPM-{request.JiraId}-{SanitizeFileName(request.Description)}.properties";
            var filePath = Path.Combine(outputDirectory, fileName);

            var content = new List<string>();
            
            // Add header comment
            content.Add("# Feature chain configuration file");
            content.Add("#");
            content.Add($"# JIRA: DEPM-{request.JiraId}");
            content.Add($"# Description: {request.Description}");
            content.Add("");
            
            // Add global properties - both versions should be the same
            if (!string.IsNullOrWhiteSpace(request.Version))
            {
                content.Add($"global.version.binary={request.Version}");
                content.Add($"global.devs.version.binary={request.Version}");
            }
            if (!string.IsNullOrWhiteSpace(request.Recipients))
            {
                content.Add($"global.recipients={request.Recipients}");
            }
            content.Add("");

            // Project order as documented in build process
            var projectOrder = new[] { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment", "tests" };
            
            foreach (var projectName in projectOrder)
            {
                var project = request.Projects.FirstOrDefault(p => p.ProjectName == projectName);
                
                if (project != null)
                {
                    // Project has feature branch - use source mode
                    content.Add($"{projectName}.mode=source");
                    
                    // Add mode.devs (commented if not selected, uncommented if selected)
                    if (!string.IsNullOrEmpty(project.DevMode))
                        content.Add($"{projectName}.mode.devs={project.DevMode}");
                    else
                        content.Add($"#{projectName}.mode.devs=binary");
                    
                    // Add fork if specified
                    if (!string.IsNullOrWhiteSpace(project.ForkRepository))
                        content.Add($"{projectName}.fork={project.ForkRepository}");
                    
                    // Add branch or tag (never both)
                    if (!string.IsNullOrWhiteSpace(project.Branch))
                    {
                        var branchName = project.Branch;
                        if (branchName == "dev")
                        {
                            branchName = $"dev/dev-DEPM-{request.JiraId}-{SanitizeFileName(request.Description)}";
                        }
                        content.Add($"{projectName}.branch={branchName}");
                    }
                    else if (!string.IsNullOrWhiteSpace(project.Tag))
                        content.Add($"{projectName}.tag={project.Tag}");
                    
                    // Add tests.unit
                    if (project.TestsEnabled && !(projectName == "content" || projectName == "deployment" || projectName == "tests" || projectName == "designer"))
                        content.Add($"{projectName}.tests.unit=true");
                    else if (projectName == "content" || projectName == "deployment" || projectName == "tests" || projectName == "designer")
                        content.Add($"{projectName}.tests.unit=false");
                }
                else
                {
                    // Project without feature branch
                    var isUpstream = IsUpstreamProject(projectName, request.Projects, projectOrder);
                    
                    if (isUpstream)
                    {
                        // Upstream projects use binary mode
                        content.Add($"{projectName}.mode=binary");
                    }
                    else
                    {
                        // Downstream projects use source mode with tag
                        content.Add($"{projectName}.mode=source");
                        
                        // Add mode.devs
                        if (projectName == "designer" || projectName == "deployment" || projectName == "tests")
                            content.Add($"#{projectName}.mode.devs=ignore");
                        else
                            content.Add($"#{projectName}.mode.devs=binary");
                        
                        // Add tag with integration build
                        if (!string.IsNullOrWhiteSpace(request.Version))
                            content.Add($"#{projectName}.tag=Build_12.25.11.{request.Version}");
                        else
                            content.Add($"#{projectName}.branch=integration");
                        
                        // Add tests.unit
                        if (projectName == "content" || projectName == "deployment" || projectName == "tests" || projectName == "designer")
                            content.Add($"{projectName}.tests.unit=false");
                        else
                            content.Add($"{projectName}.tests.unit=true");
                    }
                }
                
                content.Add("");
            }

            // Add integration tests section
            content.Add("");
            var integrationTests = new[]
            {
                "AdhocWidgetSet1", "AdhocWidgetSet2", "AdministrationService", "AppEngineService",
                "AppsProvisioning", "AppStudioService", "BusinessModelingServiceSet1", "BusinessModelingServiceSet2",
                "BusinessModelingServiceSet3", "ConsolidationService", "DashboardsService", "dEPMAppsUpdate",
                "FarmCreation", "FarmUpgrade", "OfficeIntegrationService", "OlapService", "OlapAPI",
                "ContentIntegration", "dEPMRegressionSet1", "dEPMRegressionSet2", "dEPMRegressionSet3",
                "dEPMRegressionSet4", "SelfService", "WorkforceBudgetingSet1", "WorkforceBudgetingSet2",
                "WorkforceBudgetingSet4", "WorkforceBudgetingSet5", "MultiFarm", "EPMWorkflow",
                "ModelingService", "ModelingUI", "RelationalModeling", "FinancialReportingSet1", "FinancialReportingSet2"
            };
            
            foreach (var test in integrationTests)
            {
                if (request.EnabledIntegrationTests.Contains(test))
                {
                    content.Add($"tests.{test}.run=true");
                }
                else
                {
                    content.Add($"#tests.{test}.run=true");
                }
            }

            // Remove last empty line
            if (content.Count > 0 && content.Last() == "")
                content.RemoveAt(content.Count - 1);

            File.WriteAllLines(filePath, content);
            return filePath;
        }

        public List<string> GetAvailableProjects()
        {
            return new List<string> { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment", "tests" };
        }

        public List<string> GetBranchesForProject(string projectName)
        {
            return new List<string> { "main", "develop", "stage", "integration" };
        }

        public List<string> GetForksForProject(string projectName)
        {
            return new List<string> { "user.name/" + projectName, "team.name/" + projectName };
        }

        public List<string> GetBranchesForFork(string forkName)
        {
            return new List<string> { "main", "develop", "feature/branch-name" };
        }

        public List<string> GetAvailableModes()
        {
            return new List<string> { "source", "binary", "ignore" };
        }


        


        private bool IsUpstreamProject(string projectName, List<ProjectConfig> featureProjects, string[] projectOrder)
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

        private string SanitizeFileName(string description)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("", description.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Replace(" ", "-").ToLower();
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