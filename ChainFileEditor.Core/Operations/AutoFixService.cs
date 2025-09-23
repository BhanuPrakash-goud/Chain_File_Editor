using System;
using System.Collections.Generic;
using System.Linq;
using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using ChainFileEditor.Core.Constants;

namespace ChainFileEditor.Core.Operations
{
    public sealed class AutoFixService
    {
        private static readonly string[] ValidModes = { "source", "binary", "ignore" };
        private static readonly string[] ValidDevModes = { "binary", "ignore", "source" };
        private static readonly string[] ProjectOrder = { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment", "tests" };
        
        private const string DefaultMode = "source";
        private const string DefaultDevMode = "binary";
        private const string DefaultBranch = "integration";
        private const string DefaultVersion = "10013";
        private const string StageBranch = "stage";
        private const string IgnoreMode = "ignore";
        private const string TrueValue = "true";
        private const string FalseValue = "false";
        private const string ForkSeparator = "/";
        
        private readonly ChainReorderService _reorderService = new ChainReorderService();
        public int ApplyAutoFixes(ChainModel chain, List<ValidationIssue> fixableIssues)
        {
            int fixedCount = 0;
            
            // Handle RequiredProjects issues by adding all missing projects at once
            var requiredProjectsIssues = fixableIssues.Where(i => i.RuleId == ValidationRuleIds.RequiredProjects).ToList();
            if (requiredProjectsIssues.Count > 0)
            {
                if (FixAllMissingProjects(chain))
                    fixedCount += requiredProjectsIssues.Count;
            }

            // Handle other auto-fixable issues
            foreach (var issue in fixableIssues.Where(i => i.IsAutoFixable && i.RuleId != ValidationRuleIds.RequiredProjects))
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
                return issue.RuleId switch
                {
                    ValidationRuleIds.RequiredProjects => FixAllMissingProjects(chain),
                    ValidationRuleIds.ModeRequired => FixMissingMode(chain, issue.SectionName),
                    ValidationRuleIds.ModeValidation => FixInvalidMode(chain, issue.SectionName),
                    ValidationRuleIds.BranchOrTag => FixBranchOrTag(chain, issue.SectionName, issue.Message),
                    ValidationRuleIds.BranchOrTagRequired => FixMissingBranchOrTag(chain, issue.SectionName),
                    ValidationRuleIds.ForkValidation => FixInvalidFork(chain, issue.SectionName),
                    ValidationRuleIds.ContentNotStage => FixContentStageBranch(chain, issue.SectionName),
                    ValidationRuleIds.TestsPreferBranch => FixTestsTagToBranch(chain, issue.SectionName),
                    ValidationRuleIds.DevModeOverride => FixDevModeOverride(chain, issue.SectionName),
                    ValidationRuleIds.GlobalVersionWhenBinary => FixMissingGlobalVersion(chain),
                    ValidationRuleIds.VersionRange => FixVersionRange(chain, issue.SectionName),
                    ValidationRuleIds.FeatureForkRecommendation => FixFeatureForkRecommendation(chain, issue.SectionName),
                    ValidationRuleIds.DevModeValidation => FixInvalidDevMode(chain, issue.SectionName),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        private static string ExtractProjectNameFromMessage(string message)
        {
            // Extract project name from "Required project 'projectname' is missing from chain"
            var match = System.Text.RegularExpressions.Regex.Match(message, RegexPatterns.ProjectNameExtraction);
            return match.Success ? match.Groups[1].Value : null;
        }

        private bool FixMissingMode(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null)
            {
                var mode = section.Properties.GetValueOrDefault(PropertyNames.Mode, string.Empty);
                if (string.IsNullOrWhiteSpace(mode))
                {
                    section.Properties[PropertyNames.Mode] = DefaultMode;
                    return true;
                }
            }
            return false;
        }

        private bool FixTestsTagToBranch(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null && section.Properties.ContainsKey(PropertyNames.Tag))
            {
                section.Properties.Remove(PropertyNames.Tag);
                if (!section.Properties.ContainsKey(PropertyNames.Branch))
                {
                    // Use stage branch for tests project per ValidationConfig.json
                    section.Properties[PropertyNames.Branch] = StageBranch;
                }
                return true;
            }
            return false;
        }

        private bool FixDevModeOverride(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null && section.Properties.ContainsKey(PropertyNames.DevMode) && !section.Properties.ContainsKey(PropertyNames.Mode))
            {
                section.Properties[PropertyNames.Mode] = DefaultMode;
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
                chain.Global.VersionBinary = DefaultVersion;
                return true;
            }
            
            // Validate existing version is in valid range
            if (int.TryParse(chain.Global.VersionBinary, out var version))
            {
                if (version < VersionRanges.MinVersion || version > VersionRanges.MaxVersion)
                {
                    chain.Global.VersionBinary = DefaultVersion;
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

            var hasBranch = section.Properties.ContainsKey(PropertyNames.Branch) && !string.IsNullOrWhiteSpace(section.Properties[PropertyNames.Branch]);
            var hasTag = section.Properties.ContainsKey(PropertyNames.Tag) && !string.IsNullOrWhiteSpace(section.Properties[PropertyNames.Tag]);

            if (hasBranch && hasTag)
            {
                // Remove tag, keep branch (branches preferred for development)
                section.Properties.Remove(PropertyNames.Tag);
                return true;
            }
            else if (!hasBranch && !hasTag)
            {
                // Add default branch - integration for most projects
                section.Properties[PropertyNames.Branch] = DefaultBranch;
                return true;
            }
            return false;
        }

        private static string GetDefaultBranch(string projectName)
        {
            return projectName.ToLower() switch
            {
                ProjectNames.Content => DefaultBranch,
                ProjectNames.Deployment => DefaultBranch,
                ProjectNames.Tests => StageBranch,
                ProjectNames.Designer => DefaultBranch,
                _ => DefaultBranch
            };
        }

        private bool FixContentStageBranch(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null && section.Properties.GetValueOrDefault(PropertyNames.Branch, string.Empty) == StageBranch)
            {
                section.Properties[PropertyNames.Branch] = DefaultBranch;
                return true;
            }
            return false;
        }

        private bool FixMissingBranchOrTag(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties == null) return false;

            var hasBranch = section.Properties.ContainsKey(PropertyNames.Branch) && !string.IsNullOrWhiteSpace(section.Properties[PropertyNames.Branch]);
            var hasTag = section.Properties.ContainsKey(PropertyNames.Tag) && !string.IsNullOrWhiteSpace(section.Properties[PropertyNames.Tag]);

            if (!hasBranch && !hasTag)
            {
                section.Properties[PropertyNames.Branch] = DefaultBranch;
                return true;
            }
            return false;
        }

        private bool FixInvalidFork(ChainModel chain, string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return false;
            
            var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section?.Properties != null && section.Properties.ContainsKey(PropertyNames.Fork))
            {
                var fork = section.Properties[PropertyNames.Fork];
                if (!string.IsNullOrEmpty(fork) && !fork.Contains(ForkSeparator))
                {
                    section.Properties.Remove(PropertyNames.Fork);
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
                if (version < VersionRanges.MinVersion || version > VersionRanges.StageMaxVersion)
                {
                    chain.Global.VersionBinary = DefaultVersion;
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
                var branch = section.Properties.GetValueOrDefault(PropertyNames.Branch, string.Empty);
                if (branch.StartsWith(BranchPrefixes.DevDepm) && string.IsNullOrEmpty(section.Properties.GetValueOrDefault(PropertyNames.Fork, string.Empty)))
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
                var mode = section.Properties.GetValueOrDefault(PropertyNames.Mode, string.Empty);
                if (string.IsNullOrWhiteSpace(mode) || !ValidModes.Contains(mode, StringComparer.OrdinalIgnoreCase))
                {
                    section.Properties[PropertyNames.Mode] = DefaultMode;
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
            var targetIndex = Array.IndexOf(ProjectOrder, projectName.ToLower());
            
            if (targetIndex >= 0)
            {
                // Find the correct insertion point
                int insertIndex = 0;
                for (int i = 0; i < chain.Sections.Count; i++)
                {
                    var currentIndex = Array.IndexOf(ProjectOrder, chain.Sections[i].Name.ToLower());
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
            var existingProjects = chain.Sections.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingProjects = ProjectOrder.Where(p => !existingProjects.Contains(p)).ToList();

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
                case ProjectNames.Content:
                    SetProjectProperties(newSection, DefaultMode, DefaultDevMode, DefaultBranch, FalseValue);
                    break;
                case ProjectNames.Deployment:
                    SetProjectProperties(newSection, DefaultMode, IgnoreMode, DefaultBranch, FalseValue);
                    break;
                case ProjectNames.Tests:
                    SetProjectProperties(newSection, DefaultMode, IgnoreMode, StageBranch, FalseValue);
                    break;
                case ProjectNames.Designer:
                    SetProjectProperties(newSection, DefaultMode, IgnoreMode, DefaultBranch, FalseValue);
                    break;
                default:
                    SetProjectProperties(newSection, DefaultMode, DefaultDevMode, DefaultBranch, TrueValue);
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
                var devMode = section.Properties.GetValueOrDefault(PropertyNames.DevMode, string.Empty);
                if (!string.IsNullOrEmpty(devMode) && !ValidDevModes.Contains(devMode, StringComparer.OrdinalIgnoreCase))
                {
                    section.Properties[PropertyNames.DevMode] = DefaultDevMode;
                    return true;
                }
            }
            return false;
        }

        private void ValidateCreatedSection(Section section)
        {
            // Ensure mode is valid
            if (!ValidModes.Contains(section.Properties.GetValueOrDefault(PropertyNames.Mode, string.Empty)))
                section.Properties[PropertyNames.Mode] = DefaultMode;

            // Ensure dev mode is valid
            var validDevModesWithEmpty = new[] { string.Empty }.Concat(ValidDevModes).ToArray();
            if (!validDevModesWithEmpty.Contains(section.Properties.GetValueOrDefault(PropertyNames.DevMode, string.Empty)))
                section.Properties[PropertyNames.DevMode] = DefaultDevMode;

            // Ensure branch is set
            if (string.IsNullOrWhiteSpace(section.Properties.GetValueOrDefault(PropertyNames.Branch, string.Empty)))
                section.Properties[PropertyNames.Branch] = DefaultBranch;

            // Ensure tests.unit is boolean
            var testsValue = section.Properties.GetValueOrDefault(PropertyNames.TestsUnit, FalseValue);
            if (testsValue != TrueValue && testsValue != FalseValue)
                section.Properties[PropertyNames.TestsUnit] = TrueValue;
        }
        
        private static void SetProjectProperties(Section section, string mode, string devMode, string branch, string testsUnit)
        {
            section.Properties[PropertyNames.Mode] = mode;
            section.Properties[PropertyNames.DevMode] = devMode;
            section.Properties[PropertyNames.Branch] = branch;
            section.Properties[PropertyNames.TestsUnit] = testsUnit;
        }
    }
    

}