using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ChainFileEditor.Wizard
{
    public sealed class FeatureChainWizard : WizardBase
    {
        private const string WizardName = "Feature Chain Creator";
        private const string WizardDescription = "Create a new feature chain configuration file";
        private const string DefaultMode = "source";
        private const string BinaryMode = "binary";
        private const string IgnoreMode = "ignore";
        private const string TrueValue = "true";
        private const string FalseValue = "false";
        private const string CommentPrefix = "#";
        private const string ExitCommand = "exit";
        private const string YesShort = "y";
        private const string YesLong = "yes";
        private const string NoShort = "n";
        private const string NoLong = "no";
        private const string TagShort = "t";
        private const string TagLong = "tag";
        private const string BranchShort = "b";
        private const string BranchLong = "branch";
        private const string AllCommand = "all";
        private const string NoneCommand = "none";
        private const string DevBranch = "dev";
        private const string StageBranch = "stage";
        private const string MainBranch = "main";
        private const string IntegrationBranch = "integration";
        private const string ContentProject = "content";
        private const string TestsProject = "tests";
        private const string DesignerProject = "designer";
        private const string DeploymentProject = "deployment";
        private const string FileExtension = ".properties";
        private const string SpaceReplacement = "-";
        private const string DefaultOutputDir = @"C:\ChainFileEditor\Tests\Chains";
        
        private static readonly string[] ProjectOrder = {
            "framework", "repository", "olap", "modeling", "depmservice", "consolidation",
            "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration",
            "content", "deployment", "tests"
        };
        
        private static readonly string[] ValidModes = { DefaultMode, BinaryMode, IgnoreMode };
        private static readonly string[] ValidDevModes = { BinaryMode, IgnoreMode };
        private static readonly string[] NoTestsProjects = { DesignerProject, ContentProject, DeploymentProject, TestsProject };
        private static readonly string[] IgnoreDevModeProjects = { DesignerProject, DeploymentProject, TestsProject };
        
        private static readonly string[] TestSuites = { 
            "dEPMSmoke", "dEPMRegressionSet1", "dEPMRegressionSet2", 
            "AdhocWidget", "AdministrationService", "AppEngineService", "AppsProvisioning", 
            "AppStudioService", "BusinessModelingServiceSet1", "BusinessModelingServiceSet2", 
            "ConsolidationService", "ContentIntegration", "DashboardsService", "dEPMAppsUpdate", 
            "EPMWorkflow", "FarmCreation", "FarmUpgrade", "MultiFarm", "OfficeIntegrationService", 
            "OlapService", "OlapAPI", "SelfService", "TenantClone", "WorkforceBudgetingSet1", 
            "WorkforceBudgetingSet2" 
        };
        
        public override string Name => WizardName;
        public override string Description => WizardDescription;

        public override void Execute()
        {
            ShowHeader("Feature Chain Creator Wizard");

            var jiraId = PromptForInput(PromptMessages.JiraId, required: true);
            if (jiraId == ExitCommand) return;

            var description = PromptForInput(PromptMessages.Description, required: true);
            if (description == ExitCommand) return;

            var version = PromptForInput(PromptMessages.Version, required: true);
            if (version == ExitCommand) return;

            Console.WriteLine(PromptMessages.ProjectConfigurations);
            Console.Write(PromptMessages.IncludeAllProjects);
            var includeAllInput = Console.ReadLine()?.Trim().ToLower();
            var includeAll = string.IsNullOrEmpty(includeAllInput) || includeAllInput == YesShort || includeAllInput == YesLong;
            
            var projects = new Dictionary<string, ProjectConfig>();
            var skippedProjects = new HashSet<string>();
            var fileName = $"DEPM-{jiraId}-{description.Replace(" ", SpaceReplacement).ToLower()}{FileExtension}";
            
            if (!includeAll)
            {
                Console.WriteLine(PromptMessages.SelectProjects);
                for (int i = 0; i < ProjectOrder.Length; i++)
                {
                    Console.WriteLine($"  {i + 1,2}. {ProjectOrder[i]}");
                }
                Console.Write(PromptMessages.EnterProjectNumbers);
                var selection = Console.ReadLine()?.Trim();
                
                var selectedProjects = new HashSet<string>();
                if (selection?.ToLower() == AllCommand)
                {
                    selectedProjects = ProjectOrder.ToHashSet();
                }
                else if (!string.IsNullOrEmpty(selection))
                {
                    var indices = selection.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var indexStr in indices)
                    {
                        if (int.TryParse(indexStr.Trim(), out int index) && index > 0 && index <= ProjectOrder.Length)
                        {
                            selectedProjects.Add(ProjectOrder[index - 1]);
                        }
                    }
                }
                
                // Mark unselected projects as skipped
                foreach (var project in ProjectOrder)
                {
                    if (!selectedProjects.Contains(project))
                        skippedProjects.Add(project);
                }
                
                // Configure selected projects
                foreach (var project in selectedProjects)
                {
                    var config = ConfigureProject(project, version, fileName);
                    if (config == null) return;
                    projects[project] = config;
                }
            }
            else
            {
                // Configure all projects
                foreach (var project in ProjectOrder)
                {
                    var config = ConfigureProject(project, version, fileName);
                    if (config == null) return;
                    projects[project] = config;
                }
            }

            var (enabledIntegrationTests, allIntegrationTests) = ConfigureIntegrationTests();

            var filePath = Path.Combine(DefaultOutputDir, fileName);

            GenerateFeatureChainFile(filePath, jiraId, description, version, projects, skippedProjects, enabledIntegrationTests, allIntegrationTests);

            Console.WriteLine($"\nFeature chain file created: {filePath}");
        }

        private ProjectConfig ConfigureProject(string project, string version, string fileName)
        {
            Console.WriteLine($"\n=== {project.ToUpper()} ===");
            var config = new ProjectConfig { Name = project };

            // Mode: source(default), binary, ignore, or press Enter for default
            Console.Write($"{project}.mode ({string.Join("/", ValidModes)}) [{DefaultMode}]: ");
            var modeInput = Console.ReadLine()?.Trim().ToLower();
            config.Mode = string.IsNullOrEmpty(modeInput) ? DefaultMode : 
                         (ValidModes.Contains(modeInput) ? modeInput : DefaultMode);

            // Dev mode: #(commented by default), binary, ignore
            Console.Write($"{project}.mode.devs ({CommentPrefix}/{string.Join("/", ValidDevModes)}) [{CommentPrefix}]: ");
            var devModeInput = Console.ReadLine()?.Trim().ToLower();
            
            if (string.IsNullOrEmpty(devModeInput) || devModeInput == CommentPrefix)
            {
                // Default or explicit comment - comment it out
                config.DevMode = string.Empty;
            }
            else if (ValidDevModes.Contains(devModeInput))
            {
                // Valid value entered
                config.DevMode = devModeInput;
            }
            else
            {
                // Invalid input - comment it out
                config.DevMode = string.Empty;
            }

            // Fork configuration (not for content)
            if (project != ContentProject)
            {
                Console.Write(PromptMessages.ForkPrompt);
                var forkInput = Console.ReadLine()?.Trim().ToLower();
                if (forkInput == YesShort || forkInput == YesLong)
                {
                    var knownForks = GetKnownForksForProject(project);
                    if (knownForks.Any())
                    {
                        Console.WriteLine(PromptMessages.AvailableForks);
                        for (int i = 0; i < knownForks.Count; i++)
                        {
                            Console.WriteLine($"  {i + 1}. {knownForks[i]}");
                        }
                        Console.Write(string.Format(PromptMessages.SelectFork, knownForks.Count));
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
                        Console.Write(PromptMessages.ForkName);
                        var forkName = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(forkName))
                            config.Fork = $"{forkName}/{project}";
                    }
                }
            }

            // Branch or Tag
            Console.Write("Branch or tag (b/t) [b]: ");
            var branchOrTag = Console.ReadLine()?.Trim().ToLower();
            
            if (branchOrTag == TagShort || branchOrTag == TagLong)
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
                    DevBranch => $"dev/{fileName.Replace(FileExtension, string.Empty)}",
                    StageBranch => StageBranch,
                    MainBranch => MainBranch,
                    "" => IntegrationBranch,
                    _ => branchInput.StartsWith(IntegrationBranch) ? IntegrationBranch : branchInput
                };
                
                if (branchInput == DevBranch)
                    Console.WriteLine($"  → Branch: {config.Branch}");
            }

            // Tests configuration
            var testsDefault = GetDefaultTestsEnabled(project);
            Console.Write($"{project}.tests.unit (true/false) [{(testsDefault ? TrueValue : FalseValue)}]: ");
            var testsInput = Console.ReadLine()?.Trim().ToLower();
            config.TestsUnit = string.IsNullOrEmpty(testsInput) ? testsDefault : testsInput == TrueValue;

            return config;
        }

        private (Dictionary<string, bool> enabled, Dictionary<string, bool> all) ConfigureIntegrationTests()
        {
            var enabledTests = new Dictionary<string, bool>();
            var allTests = new Dictionary<string, bool>();
            

            
            // Initialize all tests as available
            foreach (var suite in TestSuites)
                allTests[suite] = false;
            
            Console.WriteLine("\n=== INTEGRATION TESTS ===");
            Console.Write("Include integration tests [y/n] [y]: ");
            var includeTests = Console.ReadLine()?.Trim().ToLower();
            
            if (string.IsNullOrEmpty(includeTests) || includeTests == YesShort || includeTests == YesLong)
            {
                Console.WriteLine("\nSelect integration test suites:");
                for (int i = 0; i < TestSuites.Length; i++)
                {
                    Console.WriteLine($"  {i + 1,2}. {TestSuites[i]}");
                }
                Console.Write("Enter numbers (1,2,5) or 'all' or 'none': ");
                var selection = Console.ReadLine()?.Trim().ToLower();
                
                if (selection == AllCommand)
                {
                    foreach (var suite in TestSuites)
                    {
                        enabledTests[suite] = true;
                        allTests[suite] = true;
                    }
                    Console.WriteLine($"  → Selected: All {TestSuites.Length} test suites");
                }
                else if (selection != NoneCommand && !string.IsNullOrEmpty(selection))
                {
                    var indices = selection.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var selectedSuites = new List<string>();
                    foreach (var indexStr in indices)
                    {
                        if (int.TryParse(indexStr.Trim(), out int index) && index > 0 && index <= TestSuites.Length)
                        {
                            enabledTests[TestSuites[index - 1]] = true;
                            allTests[TestSuites[index - 1]] = true;
                            selectedSuites.Add(TestSuites[index - 1]);
                        }
                    }
                    if (selectedSuites.Any())
                        Console.WriteLine($"  → Selected: {string.Join(", ", selectedSuites)}");
                }
                else
                {
                    Console.WriteLine("  → No integration tests selected");
                }
            }
            else
            {
                Console.WriteLine("  → Integration tests disabled");
                return (new Dictionary<string, bool>(), new Dictionary<string, bool>());
            }
            
            return (enabledTests, allTests);
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

        private void GenerateFeatureChainFile(string filePath, string jiraId, string description, string version, Dictionary<string, ProjectConfig> projects, HashSet<string> skippedProjects, Dictionary<string, bool> enabledIntegrationTests, Dictionary<string, bool> allIntegrationTests)
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
            foreach (var projectName in ProjectOrder)
            {
                if (skippedProjects.Contains(projectName))
                {
                    // Comment out all properties for skipped projects
                    writer.WriteLine($"{CommentPrefix}{projectName}.mode={DefaultMode}");
                    writer.WriteLine($"{CommentPrefix}{projectName}.mode.devs={BinaryMode}");
                    if (projectName != ContentProject)
                        writer.WriteLine($"{CommentPrefix}{projectName}.fork=<firstname.lastname>/{projectName}");
                    writer.WriteLine($"{CommentPrefix}{projectName}.branch={IntegrationBranch}");
                    writer.WriteLine($"{CommentPrefix}{projectName}.tag=Build_12.22.9.{version}");
                    writer.WriteLine($"{CommentPrefix}{projectName}.tests.unit={GetDefaultTestsEnabled(projectName).ToString().ToLower()}");
                    writer.WriteLine();
                    continue;
                }
                
                if (!projects.ContainsKey(projectName)) continue;
                var project = projects[projectName];

                // Mode (always required)
                writer.WriteLine($"{projectName}.mode={project.Mode}");
                
                // Dev mode - only write if has value, otherwise comment
                if (!string.IsNullOrEmpty(project.DevMode))
                    writer.WriteLine($"{projectName}.mode.devs={project.DevMode}");
                else
                    writer.WriteLine($"{CommentPrefix}{projectName}.mode.devs={BinaryMode}");

                // Fork - only write if has value
                if (!string.IsNullOrEmpty(project.Fork))
                    writer.WriteLine($"{projectName}.fork={project.Fork}");

                // Branch or Tag - only write the one that has value
                if (!string.IsNullOrEmpty(project.Branch))
                    writer.WriteLine($"{projectName}.branch={project.Branch}");
                
                if (!string.IsNullOrEmpty(project.Tag))
                    writer.WriteLine($"{projectName}.tag={project.Tag}");

                // Tests configuration
                writer.WriteLine($"{projectName}.tests.unit={project.TestsUnit.ToString().ToLower()}");

                // Special comments for specific projects
                if (projectName == ContentProject)
                    writer.WriteLine(ProjectComments.ContentNoForks);
                else if (projectName == TestsProject)
                    writer.WriteLine(ProjectComments.TestsNoBranches);

                writer.WriteLine();
            }

            // Integration tests section
            if (allIntegrationTests.Any())
            {
                writer.WriteLine("# Integration Tests Configuration");
                foreach (var test in allIntegrationTests)
                {
                    if (enabledIntegrationTests.ContainsKey(test.Key) && enabledIntegrationTests[test.Key])
                        writer.WriteLine($"integration.tests.{test.Key}={TrueValue}");
                    else
                        writer.WriteLine($"{CommentPrefix}integration.tests.{test.Key}={FalseValue}");
                }
                writer.WriteLine();
            }
        }

        private static bool GetDefaultTestsEnabled(string projectName)
        {
            return !NoTestsProjects.Contains(projectName);
        }

        private static void ApplyDefaultConfiguration(ProjectConfig config, string project, string version, string defaultFork)
        {
            config.Mode = GetDefaultMode(project);
            config.DevMode = GetDefaultDevMode(project);
            config.Branch = GetDefaultBranch(project);
            config.Fork = GetDefaultFork(project) ? $"{defaultFork}/{project}" : "";
        }

        private static string GetDefaultMode(string projectName)
        {
            return DefaultMode;
        }

        private static string GetDefaultDevMode(string projectName)
        {
            return IgnoreDevModeProjects.Contains(projectName.ToLower()) ? IgnoreMode : BinaryMode;
        }

        private static string GetDefaultBranch(string projectName)
        {
            return projectName.ToLower() == TestsProject ? StageBranch : IntegrationBranch;
        }

        private static bool GetDefaultFork(string projectName)
        {
            return projectName.ToLower() != ContentProject;
        }

        private class ProjectConfig
        {
            public string Name { get; set; } = string.Empty;
            public string Mode { get; set; } = string.Empty;
            public string DevMode { get; set; } = string.Empty;
            public string Branch { get; set; } = string.Empty;
            public string Tag { get; set; } = string.Empty;
            public string Fork { get; set; } = string.Empty;
            public bool TestsUnit { get; set; } = true;
        }
    }
    
    internal static class ProjectComments
    {
        public const string ContentNoForks = "# Cannot have forks, only main repository can be used";
        public const string TestsNoBranches = "# Cannot use tags, only branches allowed";
    }
    
    internal static class PromptMessages
    {
        public const string JiraId = "JIRA ID";
        public const string Description = "Description";
        public const string Version = "Version";
        public const string ProjectConfigurations = "\nProject Configurations";
        public const string IncludeAllProjects = "Include all projects? [y] (y/n): ";
        public const string SelectProjects = "\nSelect projects to configure:";
        public const string EnterProjectNumbers = "Enter project numbers to configure (1,2,5) or 'all': ";
        public const string ForkPrompt = "Fork (y/n) [n]: ";
        public const string AvailableForks = "Available forks:";
        public const string SelectFork = "Select (1-{0}) or enter custom: ";
        public const string ForkName = "Fork name (firstname.lastname): ";
    }
}