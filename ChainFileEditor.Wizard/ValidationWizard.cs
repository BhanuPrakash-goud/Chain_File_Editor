using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;

namespace ChainFileEditor.Wizard
{
    public class ValidationWizard : WizardBase
    {
        public override string Name => "Chain Validator";
        public override string Description => "Validate chain files with detailed reporting";

        public override void Execute()
        {
            var defaultFile = @"C:\ChainFileEditor\Tests\Chains\stage.properties";
            var defaultDir = @"C:\ChainFileEditor\Tests\Chains";
            
            ShowHeader("Chain File Validation");
            Console.WriteLine("[90m▶ Select validation target[0m\n");
            
            Console.WriteLine("[33m▶[0m stage    - Validate default stage.properties file");
            Console.WriteLine("[33m▶[0m custom   - Select specific file to validate");
            Console.WriteLine("[31m▶[0m exit     - Return to main menu\n");
            
            var input = PromptForInput("Select validation target (stage/custom)", "stage");
            
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

                var rules = ValidationRuleFactory.CreateAllRules();
                var validator = new ChainValidator(rules);
                var report = validator.Validate(chain);

                DisplayValidationReport(report, filePath);

                if (report.Issues.Any(i => i.Severity == ValidationSeverity.Error))
                {
                    if (PromptForConfirmation("\nWould you like to see fix suggestions?"))
                    {
                        ShowFixSuggestions(report);
                    }
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Error validating file: {ex.Message}");
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

        private void DisplayValidationReport(ValidationReport report, string filePath)
        {
            Console.WriteLine($"\nValidation Report for: {Path.GetFileName(filePath)}");
            Console.WriteLine("═".PadRight(60, '═'));

            var errors = report.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
            var warnings = report.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();

            Console.WriteLine($"Total Issues: {report.Issues.Count}");
            Console.WriteLine($"  Errors: {errors.Count}");
            Console.WriteLine($"  Warnings: {warnings.Count}");

            if (errors.Any())
            {
                Console.WriteLine("\n❌ ERRORS:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  • {error.SectionName}: {error.Message}");
                }
            }

            if (warnings.Any())
            {
                Console.WriteLine("\n⚠️  WARNINGS:");
                foreach (var warning in warnings)
                {
                    Console.WriteLine($"  • {warning.SectionName}: {warning.Message}");
                }
            }

            if (!report.Issues.Any())
            {
                Console.WriteLine("\n✅ No issues found. Chain file is valid!");
            }
        }

        private void ShowFixSuggestions(ValidationReport report)
        {
            Console.WriteLine("\n🔧 Fix Suggestions:");
            Console.WriteLine("═".PadRight(60, '═'));

            var errorsByRule = report.Issues.Where(i => i.Severity == ValidationSeverity.Error)
                .GroupBy(i => i.RuleId);

            foreach (var group in errorsByRule)
            {
                Console.WriteLine($"\nRule: {group.Key}");
                foreach (var issue in group)
                {
                    Console.WriteLine($"  Problem: {issue.Message}");
                    Console.WriteLine($"  Suggestion: {GetFixSuggestion(issue.RuleId)}");
                }
            }
        }

        private string GetFixSuggestion(string ruleId)
        {
            return ruleId switch
            {
                "ModeRequired" => "Add a 'mode' property with value 'source', 'binary', or 'ignore'",
                "ModeValidation" => "Change mode to one of: source, binary, ignore",
                "BranchOrTag" => "Remove either branch or tag property, not both",
                "RequiredProjects" => "Add missing required projects: framework, repository, appengine, tests",
                "GlobalVersionWhenBinary" => "Add global.version.binary property when using binary mode",
                _ => "Check documentation for this validation rule"
            };
        }
    }
}