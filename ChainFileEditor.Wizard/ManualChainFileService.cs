using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChainFileEditor.Wizard
{
    public class ManualChainFileService
    {
        public class ProjectConfig
        {
            public string ProjectName { get; set; } = string.Empty;
            public string Mode { get; set; } = "source";
            public string DevMode { get; set; } = string.Empty; // Empty means commented
            public string Branch { get; set; } = string.Empty;
            public string Tag { get; set; } = string.Empty;
            public string Fork { get; set; } = string.Empty;
            public bool TestsEnabled { get; set; } = true;
        }

        public class IntegrationTestsConfig
        {
            public List<string> EnabledSuites { get; set; } = new List<string>();
        }

        public string CreateManualChainFile(string jiraId, string description, string version, List<ProjectConfig> projects, IntegrationTestsConfig integrationTests, string outputDir)
        {
            var fileName = $"dev-DEPM-{jiraId}-{SanitizeFileName(description)}.properties";
            var filePath = Path.Combine(outputDir, fileName);

            var content = new List<string>();
            
            // Add header comment
            content.Add("# Feature chain configuration file");
            content.Add("#");
            content.Add($"# JIRA: DEPM-{jiraId}");
            content.Add($"# Description: {description}");
            content.Add("");
            
            // Add global properties
            content.Add($"global.version.binary={version}");
            content.Add($"global.devs.version.binary={version}");
            content.Add("");

            // Add projects with empty lines between each project
            for (int i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                
                content.Add($"{project.ProjectName}.mode={project.Mode}");
                
                // Add mode.devs - commented if empty, otherwise uncommented
                if (string.IsNullOrEmpty(project.DevMode))
                {
                    content.Add($"#{project.ProjectName}.mode.devs=binary");
                }
                else
                {
                    content.Add($"{project.ProjectName}.mode.devs={project.DevMode}");
                }
                
                // Add fork if specified
                if (!string.IsNullOrEmpty(project.Fork))
                {
                    content.Add($"{project.ProjectName}.fork={project.Fork}");
                }
                
                // Add branch or tag (mutually exclusive)
                if (!string.IsNullOrEmpty(project.Branch))
                {
                    content.Add($"{project.ProjectName}.branch={project.Branch}");
                }
                else if (!string.IsNullOrEmpty(project.Tag))
                {
                    content.Add($"{project.ProjectName}.tag={project.Tag}");
                }
                
                // Add tests
                content.Add($"{project.ProjectName}.tests.unit={project.TestsEnabled.ToString().ToLower()}");
                
                // Add empty line after each project (including the last one)
                content.Add("");
            }

            // Add integration tests if any are enabled
            if (integrationTests?.EnabledSuites?.Any() == true)
            {
                foreach (var suite in integrationTests.EnabledSuites)
                {
                    content.Add($"tests.{suite}.run=true");
                }
                content.Add("");
            }

            File.WriteAllLines(filePath, content);
            return filePath;
        }

        private string SanitizeFileName(string description)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("", description.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Replace(" ", "-").ToLower();
        }
    }
}