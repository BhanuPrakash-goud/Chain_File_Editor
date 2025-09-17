using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChainFileEditor.Wizard
{
    public class FeatureChainWizard : WizardBase
    {
        public override string Name => "Feature Chain Creator";
        public override string Description => "Create a new feature chain configuration file";

        private readonly string[] _projectOrder = {
            "framework", "repository", "olap", "modeling", "depmservice", "consolidation",
            "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration",
            "content", "deployment", "tests"
        };

        public override void Execute()
        {
            ShowHeader("Feature Chain Creator Wizard");

            var jiraId = PromptForInput("JIRA ID (e.g., 159848)", required: true);
            if (jiraId == "exit") return;

            var description = PromptForInput("Description", required: true);
            if (description == "exit") return;

            var version = PromptForInput("Version", "10XXX", required: true);
            if (version == "exit") return;

            var outputDir = PromptForInput("Output directory", Environment.CurrentDirectory);
            if (outputDir == "exit") return;

            var projects = CollectProjectConfigurations(version);
            if (projects == null) return;

            var fileName = $"DEPM-{jiraId}-{description.Replace(" ", "-").ToLower()}.properties";
            var filePath = Path.Combine(outputDir, fileName);

            GenerateFeatureChainFile(filePath, jiraId, description, version, projects);

            Console.WriteLine($"\n[32m✓ Feature chain file created: {filePath}[0m");
        }

        private Dictionary<string, ProjectConfig> CollectProjectConfigurations(string version)
        {
            var projects = new Dictionary<string, ProjectConfig>();

            Console.WriteLine("\n[33mProject Configuration[0m");
            Console.WriteLine("Configure each project (press Enter to skip):");

            foreach (var project in _projectOrder)
            {
                Console.WriteLine($"\n--- {project.ToUpper()} ---");
                
                var include = PromptForConfirmation($"Include {project}?");
                if (!include) continue;

                var config = new ProjectConfig { Name = project };

                // Mode selection
                var modes = new[] { "source", "binary", "ignore" };
                var modeChoice = PromptForChoice($"Mode for {project}:", modes);
                if (modeChoice == -1) return null;
                config.Mode = modes[modeChoice];

                // Dev mode (only for certain projects)
                if (project != "designer" && project != "deployment" && project != "tests")
                {
                    var devModes = new[] { "binary", "ignore", "skip" };
                    var devModeChoice = PromptForChoice($"Dev mode for {project}:", devModes);
                    if (devModeChoice == -1) return null;
                    if (devModeChoice < 2) config.DevMode = devModes[devModeChoice];
                }
                else
                {
                    config.DevMode = "ignore";
                }

                // Branch or Tag
                var branchOrTag = PromptForChoice($"Use branch or tag for {project}:", new[] { "branch", "tag" });
                if (branchOrTag == -1) return null;

                if (branchOrTag == 0)
                {
                    config.Branch = PromptForInput($"Branch for {project}", "integration");
                    if (config.Branch == "exit") return null;
                }
                else if (project != "tests") // Tests cannot use tags
                {
                    config.Tag = PromptForInput($"Tag for {project}", $"Build_12.22.9.{version}");
                    if (config.Tag == "exit") return null;
                }

                // Fork (not for content project)
                if (project != "content")
                {
                    var useFork = PromptForConfirmation($"Use fork for {project}?");
                    if (useFork)
                    {
                        config.Fork = PromptForInput($"Fork for {project} (firstname.lastname/repo)");
                        if (config.Fork == "exit") return null;
                    }
                }

                projects[project] = config;
            }

            return projects;
        }

        private void GenerateFeatureChainFile(string filePath, string jiraId, string description, string version, Dictionary<string, ProjectConfig> projects)
        {
            using var writer = new StreamWriter(filePath);

            // Header comments
            writer.WriteLine("############################################## READ FIRST #################################################################");
            writer.WriteLine("# Do not invent your own propeties, properties NOT listed here are NOT supported");
            writer.WriteLine("# Mind the restrictions on projects, do NOT add properties for individual projects NOT listed for them below");
            writer.WriteLine("# Tests set to false below are meant to be set to false, do NOT bother to change it");
            writer.WriteLine("# Projects set to ignore below are meant to be set to ignore, because you cannot build them locally nor get ther binaries");
            writer.WriteLine("############################################## READ FIRST #################################################################");
            writer.WriteLine();

            // Global version
            writer.WriteLine($"global.version.binary={version}");
            writer.WriteLine($"global.devs.version.binary={version}");
            writer.WriteLine();

            // Projects in order
            foreach (var projectName in _projectOrder)
            {
                if (!projects.ContainsKey(projectName)) continue;
                var project = projects[projectName];

                writer.WriteLine($"{projectName}.mode={project.Mode}");
                
                if (!string.IsNullOrEmpty(project.DevMode))
                    writer.WriteLine($"{projectName}.mode.devs={project.DevMode}");
                else
                    writer.WriteLine($"#{projectName}.mode.devs=binary");

                if (!string.IsNullOrEmpty(project.Fork))
                    writer.WriteLine($"{projectName}.fork={project.Fork}");
                else if (projectName != "content")
                    writer.WriteLine($"#{projectName}.fork=<firstname.lastname>/{projectName}");

                if (!string.IsNullOrEmpty(project.Branch))
                    writer.WriteLine($"{projectName}.branch={project.Branch}");
                else
                    writer.WriteLine($"#{projectName}.branch=integration");

                if (!string.IsNullOrEmpty(project.Tag))
                    writer.WriteLine($"{projectName}.tag={project.Tag}");
                else
                    writer.WriteLine($"#{projectName}.tag=Build_12.22.9.{version}");

                // Tests configuration
                var testsEnabled = GetDefaultTestsEnabled(projectName);
                writer.WriteLine($"{projectName}.tests.unit={testsEnabled.ToString().ToLower()}");

                // Special comments for specific projects
                if (projectName == "content")
                    writer.WriteLine("# Cannot have forks, only main repository can be used");
                else if (projectName == "tests")
                    writer.WriteLine("# Cannot use tags, only branches allowed");

                writer.WriteLine();
            }
        }

        private bool GetDefaultTestsEnabled(string projectName)
        {
            return projectName switch
            {
                "designer" => false,
                "content" => false,
                "deployment" => false,
                "tests" => false,
                _ => true
            };
        }

        private class ProjectConfig
        {
            public string Name { get; set; } = "";
            public string Mode { get; set; } = "";
            public string DevMode { get; set; } = "";
            public string Branch { get; set; } = "";
            public string Tag { get; set; } = "";
            public string Fork { get; set; } = "";
        }
    }
}