using System;
using System.Collections.Generic;
using System.Linq;
using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;

namespace ChainFileEditor.Core.Operations
{
    public class AutoFixService
    {
        private readonly ChainReorderService _reorderService = new ChainReorderService();
        public int ApplyAutoFixes(ChainModel chain, List<ValidationIssue> fixableIssues)
        {
            int fixedCount = 0;
            
            // Handle RequiredProjects issues by adding all missing projects at once
            var requiredProjectsIssues = fixableIssues.Where(i => i.RuleId == "RequiredProjects").ToList();
            if (requiredProjectsIssues.Count > 0)
            {
                if (FixAllMissingProjects(chain))
                    fixedCount += requiredProjectsIssues.Count;
            }

            // Handle other auto-fixable issues
            foreach (var issue in fixableIssues.Where(i => i.IsAutoFixable && i.RuleId != "RequiredProjects"))
            {
                if (ApplyFix(chain, issue))
                    fixedCount++;
            }

            return fixedCount;
        }

        private bool ApplyFix(ChainModel chain, ValidationIssue issue)
        {
            try
            {
                switch (issue.RuleId)
                {
                    case "RequiredProjects":
                        return FixAllMissingProjects(chain);
                    case "ModeRequired":
                        return FixMissingMode(chain, issue.SectionName);
                    case "ModeValidation":
                        return FixInvalidMode(chain, issue.SectionName);
                    case "BranchOrTag":
                        return FixBranchOrTag(chain, issue.SectionName, issue.Message);
                    case "BranchOrTagRequired":
                        return FixMissingBranchOrTag(chain, issue.SectionName);
                    case "ForkValidation":
                        return FixInvalidFork(chain, issue.SectionName);
                    case "ContentNotStage":
                        return FixContentStageBranch(chain, issue.SectionName);
                    case "TestsPreferBranch":
                        return FixTestsTagToBranch(chain, issue.SectionName);
                    case "DevModeOverride":
                        return FixDevModeOverride(chain, issue.SectionName);
                    case "GlobalVersionWhenBinary":
                        return FixMissingGlobalVersion(chain);
                    case "VersionRange":
                        return FixVersionRange(chain, issue.SectionName);
                    case "FeatureForkRecommendation":
                        return FixFeatureForkRecommendation(chain, issue.SectionName);
                    case "DevModeValidation":
                        return FixInvalidDevMode(chain, issue.SectionName);
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private string ExtractProjectNameFromMessage(string message)
        {
            // Extract project name from "Required project 'projectname' is missing from chain"
            var match = System.Text.RegularExpressions.Regex.Match(message, @"'([^']+)'.*is missing");
            return match.Success ? match.Groups[1].Value : null;
        }

        private bool FixMissingMode(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null)
            {
                var mode = section.Properties.GetValueOrDefault("mode", "");
                if (string.IsNullOrWhiteSpace(mode))
                {
                    section.Properties["mode"] = "source";
                    return true;
                }
            }
            return false;
        }

        private bool FixTestsTagToBranch(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null && section.Properties.ContainsKey("tag"))
            {
                section.Properties.Remove("tag");
                if (!section.Properties.ContainsKey("branch"))
                {
                    // Use stage branch for tests project per ValidationConfig.json
                    section.Properties["branch"] = "stage";
                }
                return true;
            }
            return false;
        }

        private bool FixDevModeOverride(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null && section.Properties.ContainsKey("mode.devs") && !section.Properties.ContainsKey("mode"))
            {
                section.Properties["mode"] = "source";
                return true;
            }
            return false;
        }

        private bool FixMissingGlobalVersion(ChainModel chain)
        {
            if (chain.Global == null)
            {
                chain.Global = new GlobalSection();
            }
            
            // Set valid integration version range (10000-19999)
            if (string.IsNullOrEmpty(chain.Global.VersionBinary))
            {
                chain.Global.VersionBinary = "10013";
                return true;
            }
            
            // Validate existing version is in valid range
            if (int.TryParse(chain.Global.VersionBinary, out var version))
            {
                if (version < 10000 || version > 39999)
                {
                    chain.Global.VersionBinary = "10013";
                    return true;
                }
            }
            
            return false;
        }

        private bool FixBranchOrTag(ChainModel chain, string sectionName, string message)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties == null) return false;

            var hasBranch = section.Properties.ContainsKey("branch") && !string.IsNullOrWhiteSpace(section.Properties["branch"]);
            var hasTag = section.Properties.ContainsKey("tag") && !string.IsNullOrWhiteSpace(section.Properties["tag"]);

            if (hasBranch && hasTag)
            {
                // Remove tag, keep branch (branches preferred for development)
                section.Properties.Remove("tag");
                return true;
            }
            else if (!hasBranch && !hasTag)
            {
                // Add default branch - integration for most projects
                section.Properties["branch"] = "integration";
                return true;
            }
            return false;
        }

        private string GetDefaultBranch(string projectName)
        {
            return projectName.ToLower() switch
            {
                "content" => "integration",
                "deployment" => "integration", 
                "tests" => "stage",
                "designer" => "integration",
                _ => "integration"
            };
        }

        private bool FixContentStageBranch(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null && section.Properties.GetValueOrDefault("branch", "") == "stage")
            {
                section.Properties["branch"] = "integration";
                return true;
            }
            return false;
        }

        private bool FixMissingBranchOrTag(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties == null) return false;

            var hasBranch = section.Properties.ContainsKey("branch") && !string.IsNullOrWhiteSpace(section.Properties["branch"]);
            var hasTag = section.Properties.ContainsKey("tag") && !string.IsNullOrWhiteSpace(section.Properties["tag"]);

            if (!hasBranch && !hasTag)
            {
                section.Properties["branch"] = "integration";
                return true;
            }
            return false;
        }

        private bool FixInvalidFork(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null && section.Properties.ContainsKey("fork"))
            {
                var fork = section.Properties["fork"];
                if (!string.IsNullOrEmpty(fork) && !fork.Contains("/"))
                {
                    section.Properties.Remove("fork");
                    return true;
                }
            }
            return false;
        }

        private bool FixVersionRange(ChainModel chain, string sectionName)
        {
            if (chain.Global == null) chain.Global = new GlobalSection();
            
            if (!string.IsNullOrEmpty(chain.Global.VersionBinary) && int.TryParse(chain.Global.VersionBinary, out var version))
            {
                if (version < 10000 || version > 30000)
                {
                    chain.Global.VersionBinary = "20013";
                    return true;
                }
            }
            return false;
        }

        private bool FixFeatureForkRecommendation(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null)
            {
                var branch = section.Properties.GetValueOrDefault("branch", "");
                if (branch.StartsWith("dev/DEPM-") && string.IsNullOrEmpty(section.Properties.GetValueOrDefault("fork", "")))
                {
                    // Don't auto-add fork as it requires user decision, just return false
                    return false;
                }
            }
            return false;
        }

        private bool FixInvalidMode(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null)
            {
                var mode = section.Properties.GetValueOrDefault("mode", "");
                var validModes = new[] { "source", "binary", "ignore" };
                
                if (string.IsNullOrWhiteSpace(mode) || !validModes.Contains(mode, StringComparer.OrdinalIgnoreCase))
                {
                    section.Properties["mode"] = "source";
                    return true;
                }
            }
            return false;
        }

        private bool FixMissingProject(ChainModel chain, string projectName)
        {
            if (string.IsNullOrEmpty(projectName)) return false;
            
            if (chain.Sections == null)
                chain.Sections = new List<Section>();

            var existingProject = chain.Sections.FirstOrDefault(s => s.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
            if (existingProject != null)
                return false;

            var newSection = CreateProjectSection(projectName);
            
            // Insert in correct position based on template order
            var projectOrder = new[] { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment", "tests" };
            var targetIndex = Array.IndexOf(projectOrder, projectName.ToLower());
            
            if (targetIndex >= 0)
            {
                // Find the correct insertion point
                int insertIndex = 0;
                for (int i = 0; i < chain.Sections.Count; i++)
                {
                    var currentIndex = Array.IndexOf(projectOrder, chain.Sections[i].Name.ToLower());
                    if (currentIndex < 0 || currentIndex > targetIndex)
                        break;
                    insertIndex = i + 1;
                }
                chain.Sections.Insert(insertIndex, newSection);
            }
            else
            {
                chain.Sections.Add(newSection);
            }
            
            System.Diagnostics.Debug.WriteLine($"Added missing project: {projectName}");
            return true;
        }

        private bool FixAllMissingProjects(ChainModel chain)
        {
            if (chain.Sections == null)
                chain.Sections = new List<Section>();

            // Use the same project order as the template
            var projectOrder = new[] { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment", "tests" };
            var existingProjects = chain.Sections.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingProjects = projectOrder.Where(p => !existingProjects.Contains(p)).ToList();

            // Add missing projects
            foreach (var projectName in missingProjects)
            {
                var newSection = CreateProjectSection(projectName);
                chain.Sections.Add(newSection);
                System.Diagnostics.Debug.WriteLine($"Added missing project: {projectName}");
            }

            // Reorder all sections to match template order
            _reorderService.ReorderChain(chain);

            return missingProjects.Count > 0;
        }

        public void FixAllIssues(ChainModel chain)
        {
            var validator = new ChainValidator(ValidationRuleFactory.CreateAllRules());
            var errors = validator.Validate(chain).Issues.Where(x => x.Severity == ValidationSeverity.Error && x.IsAutoFixable).ToList();
            ApplyAutoFixes(chain, errors);
            
            // Always reorder sections to match template
            _reorderService.ReorderChain(chain);
        }

        private Section CreateProjectSection(string projectName)
        {
            var newSection = new Section
            {
                Name = projectName,
                Properties = new Dictionary<string, string>()
            };

            // Set default configuration based on template with validation
            switch (projectName.ToLower())
            {
                case "content":
                    newSection.Properties["mode"] = "source";
                    newSection.Properties["mode.devs"] = "binary";
                    newSection.Properties["branch"] = "integration";
                    newSection.Properties["tests.unit"] = "false";
                    break;
                case "deployment":
                    newSection.Properties["mode"] = "source";
                    newSection.Properties["mode.devs"] = "ignore";
                    newSection.Properties["branch"] = "integration";
                    newSection.Properties["tests.unit"] = "false";
                    break;
                case "tests":
                    newSection.Properties["mode"] = "source";
                    newSection.Properties["mode.devs"] = "ignore";
                    newSection.Properties["branch"] = "stage";
                    newSection.Properties["tests.unit"] = "false";
                    break;
                case "designer":
                    newSection.Properties["mode"] = "source";
                    newSection.Properties["mode.devs"] = "ignore";
                    newSection.Properties["branch"] = "integration";
                    newSection.Properties["tests.unit"] = "false";
                    break;
                default:
                    newSection.Properties["mode"] = "source";
                    newSection.Properties["mode.devs"] = "binary";
                    newSection.Properties["branch"] = "integration";
                    newSection.Properties["tests.unit"] = "true";
                    break;
            }

            // Validate created section meets business rules
            ValidateCreatedSection(newSection);
            return newSection;
        }

        private bool FixInvalidDevMode(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null)
            {
                var devMode = section.Properties.GetValueOrDefault("mode.devs", "");
                var validDevModes = new[] { "binary", "ignore", "source" };
                
                if (!string.IsNullOrEmpty(devMode) && !validDevModes.Contains(devMode, StringComparer.OrdinalIgnoreCase))
                {
                    section.Properties["mode.devs"] = "binary";
                    return true;
                }
            }
            return false;
        }

        private void ValidateCreatedSection(Section section)
        {
            // Ensure mode is valid
            var validModes = new[] { "source", "binary", "ignore" };
            if (!validModes.Contains(section.Properties.GetValueOrDefault("mode", "")))
                section.Properties["mode"] = "source";

            // Ensure dev mode is valid
            var validDevModes = new[] { "", "binary", "ignore", "source" };
            if (!validDevModes.Contains(section.Properties.GetValueOrDefault("mode.devs", "")))
                section.Properties["mode.devs"] = "binary";

            // Ensure branch is set
            if (string.IsNullOrWhiteSpace(section.Properties.GetValueOrDefault("branch", "")))
                section.Properties["branch"] = "integration";

            // Ensure tests.unit is boolean
            var testsValue = section.Properties.GetValueOrDefault("tests.unit", "false");
            if (testsValue != "true" && testsValue != "false")
                section.Properties["tests.unit"] = "true";
        }
    }
}