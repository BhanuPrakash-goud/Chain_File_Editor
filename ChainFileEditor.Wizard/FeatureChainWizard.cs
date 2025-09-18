using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

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

            var jiraId = PromptForInput("JIRA ID", required: true);
            if (jiraId == "exit") return;

            var description = PromptForInput("Description", required: true);
            if (description == "exit") return;

            var version = PromptForInput("Version", required: true);
            if (version == "exit") return;

            Console.WriteLine("\nProject Configurations");
            var includeAll = PromptForConfirmation("Include all projects? [y]");
            if (includeAll == false) includeAll = true; // Default to true
            
            var projects = new Dictionary<string, ProjectConfig>();
            var fileName = $"DEPM-{jiraId}-{description.Replace(" ", "-").ToLower()}.properties";
            
            foreach (var project in _projectOrder)
            {
                if (!includeAll)
                {
                    var include = PromptForConfirmation($"Include {project}?");
                    if (!include) continue;
                }
                
                var config = ConfigureProject(project, version, fileName);
                if (config == null) return;
                projects[project] = config;
            }

            var integrationTests = ConfigureIntegrationTests();

            var outputDir = @"C:\ChainFileEditor\Tests\Chains";
            var filePath = Path.Combine(outputDir, fileName);

            GenerateFeatureChainFile(filePath, jiraId, description, version, projects, integrationTests);

            Console.WriteLine($"\nFeature chain file created: {filePath}");
        }

        private ProjectConfig ConfigureProject(string project, string version, string fileName)
        {
            Console.WriteLine($"\n=== {project.ToUpper()} ===");
            var config = new ProjectConfig { Name = project };

            // Mode: source(default), binary, ignore, or press Enter for default
            Console.Write($"{project}.mode (source/binary/ignore) [source]: ");
            var modeInput = Console.ReadLine()?.Trim().ToLower();
            config.Mode = string.IsNullOrEmpty(modeInput) ? "source" : 
                         (new[] { "source", "binary", "ignore" }.Contains(modeInput) ? modeInput : "source");

            // Dev mode: #(commented), binary, ignore
            var devModeDefault = GetDefaultDevMode(project);
            var devModePrompt = devModeDefault == "ignore" ? "#" : devModeDefault;
            Console.Write($"{project}.mode.devs (#/binary/ignore) [{devModePrompt}]: ");
            var devModeInput = Console.ReadLine()?.Trim().ToLower();
            
            if (string.IsNullOrEmpty(devModeInput))
                config.DevMode = devModeDefault == "ignore" ? "" : devModeDefault;
            else if (devModeInput == "#")
                config.DevMode = "";
            else
                config.DevMode = new[] { "binary", "ignore" }.Contains(devModeInput) ? devModeInput : devModeDefault;

            // Fork configuration (not for content)
            if (project != "content")
            {
                Console.Write("Fork (y/n) [n]: ");
                var forkInput = Console.ReadLine()?.Trim().ToLower();
                if (forkInput == "y" || forkInput == "yes")
                {
                    var knownForks = GetKnownForksForProject(project);
                    if (knownForks.Any())
                    {
                        Console.WriteLine("Available forks:");
                        for (int i = 0; i < knownForks.Count; i++)
                        {
                            Console.WriteLine($"  {i + 1}. {knownForks[i]}");
                        }
                        Console.Write($"Select (1-{knownForks.Count}) or enter custom: ");
                        var forkChoice = Console.ReadLine()?.Trim();
                        
                        if (int.TryParse(forkChoice, out int index) && index > 0 && index <= knownForks.Count)
                        {
                            config.Fork = $"{knownForks[index - 1]}/{project}";
                        }
                        else if (!string.IsNullOrEmpty(forkChoice))
                        {
                            config.Fork = $"{forkChoice}/{project}";
                        }
                    }
                    else
                    {
                        Console.Write("Fork name (firstname.lastname): ");
                        var forkName = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(forkName))
                            config.Fork = $"{forkName}/{project}";
                    }
                }
            }

            // Branch or Tag
            Console.Write("Branch or tag (b/t) [b]: ");
            var branchOrTag = Console.ReadLine()?.Trim().ToLower();
            
            if (branchOrTag == "t" || branchOrTag == "tag")
            {
                var currentDate = DateTime.Now.ToString("MM.dd.yy");
                config.Tag = $"Build_{currentDate}.{version}";
                Console.WriteLine($"  → Tag: {config.Tag}");
            }
            else
            {
                Console.Write("Branch (integration/stage/main/dev) [integration]: ");
                var branchInput = Console.ReadLine()?.Trim().ToLower();
                
                config.Branch = branchInput switch
                {
                    "dev" => $"dev/{fileName.Replace(".properties", "")}",
                    "stage" => "stage",
                    "main" => "main",
                    "" => "integration",
                    _ => branchInput.StartsWith("integration") ? "integration" : branchInput
                };
                
                if (branchInput == "dev")
                    Console.WriteLine($"  → Branch: {config.Branch}");
            }

            // Tests configuration
            var testsDefault = GetDefaultTestsEnabled(project);
            Console.Write($"{project}.tests.unit (true/false) [{(testsDefault ? "true" : "false")}]: ");
            var testsInput = Console.ReadLine()?.Trim().ToLower();
            config.TestsUnit = string.IsNullOrEmpty(testsInput) ? testsDefault : testsInput == "true";

            return config;
        }

        private Dictionary<string, bool> ConfigureIntegrationTests()
        {
            var integrationTests = new Dictionary<string, bool>();
            
            Console.WriteLine("\n=== INTEGRATION TESTS ===");
            Console.Write("Integration tests (n/smoke/regression/all/custom) [n]: ");
            var choice = Console.ReadLine()?.Trim().ToLower();
            
            var testSuites = new[] { 
                "dEPMSmoke", "dEPMRegressionSet1", "dEPMRegressionSet2", 
                "AdhocWidget", "AdministrationService", "AppEngineService", "AppsProvisioning", 
                "AppStudioService", "BusinessModelingServiceSet1", "BusinessModelingServiceSet2", 
                "ConsolidationService", "ContentIntegration", "DashboardsService", "dEPMAppsUpdate", 
                "EPMWorkflow", "FarmCreation", "FarmUpgrade", "MultiFarm", "OfficeIntegrationService", 
                "OlapService", "OlapAPI", "SelfService", "TenantClone", "WorkforceBudgetingSet1", 
                "WorkforceBudgetingSet2" 
            };
            
            switch (choice)
            {
                case "smoke" or "s":
                    integrationTests["dEPMSmoke"] = true;
                    Console.WriteLine("  → Selected: dEPMSmoke");
                    break;
                case "regression" or "r":
                    integrationTests["dEPMRegressionSet1"] = true;
                    integrationTests["dEPMRegressionSet2"] = true;
                    Console.WriteLine("  → Selected: dEPMRegressionSet1, dEPMRegressionSet2");
                    break;
                case "all" or "a":
                    foreach (var suite in testSuites)
                        integrationTests[suite] = true;
                    Console.WriteLine($"  → Selected: All {testSuites.Length} test suites");
                    break;
                case "custom" or "c":
                    Console.WriteLine("Available test suites:");
                    for (int i = 0; i < testSuites.Length; i++)
                    {
                        Console.WriteLine($"  {i + 1,2}. {testSuites[i]}");
                    }
                    Console.Write("Enter numbers (1,2,5): ");
                    var selection = Console.ReadLine()?.Trim();
                    
                    if (!string.IsNullOrEmpty(selection))
                    {
                        var indices = selection.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        var selectedSuites = new List<string>();
                        foreach (var indexStr in indices)
                        {
                            if (int.TryParse(indexStr.Trim(), out int index) && index > 0 && index <= testSuites.Length)
                            {
                                integrationTests[testSuites[index - 1]] = true;
                                selectedSuites.Add(testSuites[index - 1]);
                            }
                        }
                        if (selectedSuites.Any())
                            Console.WriteLine($"  → Selected: {string.Join(", ", selectedSuites)}");
                    }
                    break;
                case "" or "n" or "no":
                    Console.WriteLine("  → No integration tests");
                    break;
            }
            
            return integrationTests;
        }
        
        private List<string> GetKnownForksForProject(string project)
        {
            var knownForks = new Dictionary<string, string[]>
            {
                ["dasa.petrezselyova"] = new[] { "dashboards", "modeling" },
                ["ivan.rebo"] = new[] { "administration", "appengine", "appstudio", "depmservice", "consolidation", "dashboards", "framework", "modeling", "officeinteg", "olap", "repository" },
                ["oliver.schmidt"] = new[] { "framework", "olap" },
                ["petr.novacek"] = new[] { "administration", "appengine", "appstudio", "depmservice", "consolidation", "dashboards", "deployment", "framework", "modeling", "officeinteg", "olap", "repository" },
                ["stefan.kiel"] = new[] { "framework", "olap" },
                ["vit.holy"] = new[] { "appstudio", "framework", "officeinteg" },
                ["vojtech.lahoda"] = new[] { "appengine", "consolidation", "depmservice", "framework", "modeling" }
            };
            
            return knownForks.Where(kv => kv.Value.Contains(project))
                           .Select(kv => kv.Key)
                           .ToList();
        }

        private void GenerateFeatureChainFile(string filePath, string jiraId, string description, string version, Dictionary<string, ProjectConfig> projects, Dictionary<string, bool> integrationTests)
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
                var testsEnabled = project.TestsUnit;
                writer.WriteLine($"{projectName}.tests.unit={testsEnabled.ToString().ToLower()}");

                // Special comments for specific projects
                if (projectName == "content")
                    writer.WriteLine("# Cannot have forks, only main repository can be used");
                else if (projectName == "tests")
                    writer.WriteLine("# Cannot use tags, only branches allowed");

                writer.WriteLine();
            }

            // Integration tests section
            if (integrationTests.Any(t => t.Value))
            {
                writer.WriteLine("# Integration Tests Configuration");
                foreach (var test in integrationTests.Where(t => t.Value))
                {
                    writer.WriteLine($"integration.tests.{test.Key}=true");
                }
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

        private void ApplyDefaultConfiguration(ProjectConfig config, string project, string version, string defaultFork)
        {
            config.Mode = GetDefaultMode(project);
            config.DevMode = GetDefaultDevMode(project);
            config.Branch = GetDefaultBranch(project);
            config.Fork = GetDefaultFork(project) ? $"{defaultFork}/{project}" : "";
        }

        private string GetDefaultMode(string projectName)
        {
            return "source";
        }

        private string GetDefaultDevMode(string projectName)
        {
            return projectName.ToLower() switch
            {
                "designer" => "ignore",
                "deployment" => "ignore",
                "tests" => "ignore",
                _ => "binary"
            };
        }

        private string GetDefaultBranch(string projectName)
        {
            return projectName.ToLower() switch
            {
                "tests" => "stage",
                _ => "integration"
            };
        }

        private bool GetDefaultFork(string projectName)
        {
            return projectName.ToLower() != "content";
        }

        private class ProjectConfig
        {
            public string Name { get; set; } = "";
            public string Mode { get; set; } = "";
            public string DevMode { get; set; } = "";
            public string Branch { get; set; } = "";
            public string Tag { get; set; } = "";
            public string Fork { get; set; } = "";
            public bool TestsUnit { get; set; } = true;
        }
    }
}