using System;
using System.IO;
using System.Linq;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using ChainFileEditor.Core.Models;

namespace ChainFileEditor.Wizard
{
    public class AutoFixWizard : WizardBase
    {
        public override string Name => "Auto Fix";
        public override string Description => "Validate and automatically fix chain file issues";

        public override void Execute()
        {
            ShowHeader("Chain File Validation & Auto-Fix Wizard");

            var filePath = PromptForInput("Chain file path", required: true);
            if (filePath == "exit" || !File.Exists(filePath))
            {
                Console.WriteLine("[31m✗ File not found[0m");
                return;
            }

            try
            {
                var parser = new ChainFileParser();
                var chain = parser.ParsePropertiesFile(filePath);

                var rules = ValidationRuleFactory.CreateAllRules();
                var validator = new ChainValidator(rules);

                // Run validation
                Console.WriteLine("\n[33m▶ Running validation...[0m");
                var report = validator.Validate(chain);
                var errors = report.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
                var warnings = report.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
                var fixableErrors = errors.Where(i => i.IsAutoFixable).ToList();

                // Show validation results
                Console.WriteLine($"\n[36mValidation Results:[0m");
                Console.WriteLine($"  Total Issues: {report.Issues.Count}");
                Console.WriteLine($"  Errors: {errors.Count}");
                Console.WriteLine($"  Warnings: {warnings.Count}");
                Console.WriteLine($"  Auto-fixable: {fixableErrors.Count}");
                Console.WriteLine($"  Status: {(errors.Count == 0 ? "[32mVALID[0m" : "[31mINVALID[0m")}");

                if (report.Issues.Count == 0)
                {
                    Console.WriteLine("\n[32m✓ Chain file is valid![0m");
                    return;
                }

                // Show issues
                if (errors.Count > 0)
                {
                    Console.WriteLine("\n[31mERRORS:[0m");
                    foreach (var error in errors.Take(5))
                    {
                        Console.WriteLine($"  [31m✗[0m {error.SectionName ?? "Global"}: {error.Message}");
                        if (error.IsAutoFixable)
                            Console.WriteLine($"    [32m→[0m Auto-fixable");
                    }
                    if (errors.Count > 5)
                        Console.WriteLine($"    ... and {errors.Count - 5} more errors");
                }

                if (warnings.Count > 0)
                {
                    Console.WriteLine("\n[33mWARNINGS:[0m");
                    foreach (var warning in warnings.Take(3))
                    {
                        Console.WriteLine($"  [33m⚠[0m {warning.SectionName ?? "Global"}: {warning.Message}");
                    }
                    if (warnings.Count > 3)
                        Console.WriteLine($"    ... and {warnings.Count - 3} more warnings");
                }

                // Auto-fix option
                if (fixableErrors.Count > 0)
                {
                    Console.WriteLine();
                    if (PromptForConfirmation($"Auto-fix {fixableErrors.Count} errors?"))
                    {
                        var autoFixService = new AutoFixService();
                        Console.WriteLine("\n[33m▶ Applying auto-fixes...[0m");
                        var fixedCount = autoFixService.ApplyAutoFixes(chain, fixableErrors);

                        var writer = new ChainFileWriter();
                        writer.WritePropertiesFile(filePath, chain);

                        Console.WriteLine($"[32m✓ Fixed {fixedCount} issues[0m");
                        
                        // Re-validate
                        var finalChain = parser.ParsePropertiesFile(filePath);
                        var finalReport = validator.Validate(finalChain);
                        var remainingErrors = finalReport.Issues.Count(i => i.Severity == ValidationSeverity.Error);
                        
                        Console.WriteLine($"  Remaining errors: {remainingErrors}");
                        Console.WriteLine($"  Final status: {(remainingErrors == 0 ? "[32mVALID[0m" : "[33mNEEDS ATTENTION[0m")}");
                    }
                }
                else if (errors.Count > 0)
                {
                    Console.WriteLine("\n[33m! No auto-fixable errors found. Manual fixes required.[0m");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[31m✗ Error: {ex.Message}[0m");
            }
        }
    }
}