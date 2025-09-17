using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Validation.Rules;
using ChainFileEditor.Core.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChainFileEditor.Console.Commands
{
    public class ValidateCommand : CommandBase
    {
        public override string Name => "validate";
        public override string Description => "Validates chain file against all rules. Use --auto-fix to automatically fix issues.";

        public override async Task<int> ExecuteAsync(string[] args)
        {
            try
            {
                var chainFile = GetArgument(args, "--chain-file");
                var autoFix = args.Contains("--auto-fix");

                if (string.IsNullOrEmpty(chainFile))
                {
                    System.Console.Write("Enter chain file path: ");
                    chainFile = System.Console.ReadLine();
                    
                    if (string.IsNullOrEmpty(chainFile))
                    {
                        WriteError("File path is required");
                        return 1;
                    }
                }

                if (!File.Exists(chainFile))
                {
                    WriteError($"Chain file not found: {chainFile}");
                    return 1;
                }

                var parser = new ChainFileParser();
                var chain = parser.ParsePropertiesFile(chainFile);

                var rules = ValidationRuleFactory.CreateAllRules();
                var validator = new ChainValidator(rules);

                var result = validator.Validate(chain);
                
                // Apply auto-fixes if requested
                if (autoFix)
                {
                    var fixableIssues = result.Issues.Where(i => i.IsAutoFixable).ToList();
                    if (fixableIssues.Count > 0)
                    {
                        System.Console.WriteLine($"Applying {fixableIssues.Count} auto-fixes...");
                        var autoFixService = new AutoFixService();
                        var fixedCount = autoFixService.ApplyAutoFixes(chain, fixableIssues);
                        
                        if (fixedCount > 0)
                        {
                            var writer = new ChainFileWriter();
                            writer.WritePropertiesFile(chainFile, chain);
                            System.Console.WriteLine($"Fixed {fixedCount} issues and saved file.");
                            
                            // Re-validate after fixes
                            result = validator.Validate(chain);
                        }
                    }
                }
                
                var report = new ValidationReport(result.Issues.ToList());
                DisplayDetailedValidation(report, chain, chainFile);
                
                var errorCount = result.Issues.Count(i => i.Severity == ValidationSeverity.Error);
                return errorCount > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
                return 1;
            }
        }

        private string GetArgument(string[] args, string name)
        {
            var arg = args.FirstOrDefault(a => a.StartsWith($"{name}="));
            return arg?.Substring(name.Length + 1) ?? string.Empty;
        }
        
        private void DisplayDetailedValidation(ValidationReport report, ChainFileEditor.Core.Models.ChainModel chain, string filePath)
        {
            var errorCount = report.Issues.Count(i => i.Severity == ValidationSeverity.Error);
            var warningCount = report.Issues.Count(i => i.Severity == ValidationSeverity.Warning);
            var totalIssues = report.Issues.Count();
            
            System.Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
            System.Console.WriteLine("                              VALIDATION SUMMARY");
            System.Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
            System.Console.WriteLine();
            
            System.Console.WriteLine($"File: {Path.GetFileName(filePath)}");
            System.Console.WriteLine($"Validation Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            System.Console.WriteLine($"Total Issues Found: {totalIssues}");
            System.Console.WriteLine($"  • Errors: {errorCount}");
            System.Console.WriteLine($"  • Warnings: {warningCount}");
            
            var status = errorCount > 0 ? "FAILED" : (warningCount > 0 ? "PASSED WITH WARNINGS" : "PASSED");
            System.Console.ForegroundColor = errorCount > 0 ? ConsoleColor.Red : (warningCount > 0 ? ConsoleColor.Yellow : ConsoleColor.Green);
            System.Console.WriteLine($"Overall Status: {status}");
            System.Console.ResetColor();
            System.Console.WriteLine();
            
            System.Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
            System.Console.WriteLine("                             PROJECT ANALYSIS");
            System.Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
            System.Console.WriteLine();
            
            System.Console.WriteLine($"Global Configuration:");
            System.Console.WriteLine($"  • Global Version: {chain.Global?.Version ?? "Not specified"}");
            System.Console.WriteLine($"  • Global Description: {chain.Global?.Description ?? "Not specified"}");
            System.Console.WriteLine($"  • JIRA ID: {chain.Global?.JiraId ?? "Not specified"}");
            System.Console.WriteLine();
            
            System.Console.WriteLine($"Projects Found: {chain.Sections?.Count ?? 0}");
            if (chain.Sections?.Any() == true)
            {
                foreach (var section in chain.Sections.Take(5))
                {
                    System.Console.WriteLine($"  • {section.Name}:");
                    System.Console.WriteLine($"    - Mode: {section.Mode ?? "Not specified"}");
                    System.Console.WriteLine($"    - Branch: {section.Branch ?? "Not specified"}");
                    System.Console.WriteLine($"    - Tag: {section.Tag ?? "Not specified"}");
                    System.Console.WriteLine($"    - Tests: {(section.TestsUnit ? "Enabled" : "Disabled")}");
                }
                
                if (chain.Sections.Count > 5)
                {
                    System.Console.WriteLine($"  ... and {chain.Sections.Count - 5} more projects");
                }
            }
            System.Console.WriteLine();
            
            if (totalIssues > 0)
            {
                System.Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                System.Console.WriteLine("                            ERRORS AND WARNINGS");
                System.Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                System.Console.WriteLine();
                
                var errorIssues = report.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
                var warningIssues = report.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
                
                if (errorIssues.Any())
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"🔴 ERRORS ({errorIssues.Count}):");
                    System.Console.ResetColor();
                    System.Console.WriteLine("───────────────────────────────────────────────────────────────────────────────");
                    
                    for (int i = 0; i < errorIssues.Count; i++)
                    {
                        var issue = errorIssues[i];
                        System.Console.WriteLine($"{i + 1}. Rule: {issue.RuleId}");
                        System.Console.WriteLine($"   Section: {issue.SectionName ?? "Global"}");
                        System.Console.WriteLine($"   Issue: {issue.Message}");
                        System.Console.WriteLine();
                    }
                }
                
                if (warningIssues.Any())
                {
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine($"🟡 WARNINGS ({warningIssues.Count}):");
                    System.Console.ResetColor();
                    System.Console.WriteLine("───────────────────────────────────────────────────────────────────────────────");
                    
                    for (int i = 0; i < warningIssues.Count; i++)
                    {
                        var issue = warningIssues[i];
                        System.Console.WriteLine($"{i + 1}. Rule: {issue.RuleId}");
                        System.Console.WriteLine($"   Section: {issue.SectionName ?? "Global"}");
                        System.Console.WriteLine($"   Issue: {issue.Message}");
                        System.Console.WriteLine();
                    }
                }
            }
            else
            {
                System.Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                System.Console.WriteLine("                               ALL CHECKS PASSED");
                System.Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("✅ No validation issues found. The chain file is valid!");
                System.Console.ResetColor();
                System.Console.WriteLine();
            }
            
            System.Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
            System.Console.WriteLine("                              END OF REPORT");
            System.Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        }
    }
}