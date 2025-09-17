using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Operations;
using static ChainFileEditor.Core.Operations.RebaseService;

namespace ChainFileEditor.Wizard
{
    public class RebaseWizard : WizardBase
    {
        public override string Name => "Version Rebase";
        public override string Description => "Update versions across multiple projects";

        public override void Execute()
        {
            var defaultFile = @"C:\ChainFileEditor\Tests\Chains\stage.properties";
            var defaultDir = @"C:\ChainFileEditor\Tests\Chains";
            
            ShowHeader("Version Rebase Operation");
            Console.WriteLine("[90m▶ Select target file for version update[0m\n");
            
            Console.WriteLine("[33m▶[0m stage    - Rebase default stage.properties file");
            Console.WriteLine("[33m▶[0m custom   - Select specific file to rebase");
            Console.WriteLine("[31m▶[0m exit     - Return to main menu\n");
            
            var input = PromptForInput("Select rebase target (stage/custom)", "stage");
            
            if (input == "exit") return;
            
            string filePath;
            if (input.ToLower() == "custom" || input == "c")
            {
                var filename = PromptForInput("Enter filename (from C:\\ChainFileEditor\\Tests\\Chains)", "", true);
                if (filename == "exit") return;
                filePath = Path.Combine(defaultDir, filename);
            }
            else
            {
                filePath = defaultFile;
            }
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

            try
            {
                var parser = new ChainFileParser();
                var chain = parser.ParsePropertiesFile(filePath);

                var rebaseService = new RebaseService();
                var currentVersion = rebaseService.ExtractCurrentVersion(chain);
                
                Console.WriteLine($"Current version: {currentVersion ?? "Not found"}");
                Console.Write("New version: ");
                var newVersion = Console.ReadLine();
                
                if (newVersion?.ToLower() == "exit") return;
                if (string.IsNullOrEmpty(newVersion))
                {
                    Console.WriteLine("Version is required. Exiting.");
                    return;
                }
                
                var updatedCount = rebaseService.UpdateAllProjects(chain, newVersion);
                
                var writer = new ChainFileWriter();
                writer.WritePropertiesFile(filePath, chain);
                Console.WriteLine("Rebase successful!");

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Error during rebase: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private string PromptForFilePath()
        {
            var currentDir = Environment.CurrentDirectory;
            var chainFiles = Directory.GetFiles(currentDir, "*.properties", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(currentDir, "*.chain", SearchOption.TopDirectoryOnly))
                .ToArray();

            if (chainFiles.Any())
            {
                Console.WriteLine("Found chain files in current directory:");
                for (int i = 0; i < chainFiles.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(chainFiles[i])}");
                }
                Console.WriteLine($"{chainFiles.Length + 1}. Enter custom path");

                var options = chainFiles.Select(f => Path.GetFileName(f) ?? "").Concat(new[] { "Enter custom path" }).ToArray();
                var choice = PromptForChoice("Select file:", options);

                if (choice < chainFiles.Length)
                {
                    return chainFiles[choice];
                }
            }

            return PromptForInput("Enter file path");
        }

        private List<string> SelectProjects(List<ProjectVersionInfo> projectVersions)
        {
            Console.WriteLine("\nSelect projects to update:");
            Console.WriteLine("═".PadRight(40, '═'));

            var selectedProjects = new List<string>();

            foreach (var project in projectVersions)
            {
                Console.WriteLine($"\nProject: {project.ProjectName}");
                Console.WriteLine($"Type: {project.PropertyType}, Value: {project.CurrentValue}");
                
                if (PromptForConfirmation($"Update {project.ProjectName}?"))
                {
                    selectedProjects.Add(project.ProjectName);
                }
            }

            if (selectedProjects.Any())
            {
                Console.WriteLine($"\nSelected projects: {string.Join(", ", selectedProjects)}");
            }

            return selectedProjects;
        }
    }
}