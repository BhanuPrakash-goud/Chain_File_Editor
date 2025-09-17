using ChainFileEditor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class RequiredProjectsRule : ValidationRuleBase
    {
        private readonly string[] _requiredProjects = { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment", "tests" };

        public override string RuleId => "RequiredProjects";
        public override string Description => "Validates that required projects are present";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var existingProjects = chain.Sections?.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
            var missingProjects = _requiredProjects.Where(p => !existingProjects.Contains(p)).ToList();

            // Create individual auto-fixable issues for each missing project
            foreach (var missingProject in missingProjects)
            {
                // Try creating with explicit parameter values to debug
                var ruleId = "RequiredProjects";
                var message = $"Required project '{missingProject}' is missing from chain";
                var severity = ValidationSeverity.Error;
                var sectionName = missingProject;
                var isAutoFixable = true;
                var suggestedFix = $"Add {missingProject} project with default configuration";
                
                var issue = new ValidationIssue(ruleId, sectionName, severity, message, isAutoFixable, suggestedFix);
                
                result.AddIssue(issue);
            }

            return result;
        }
    }
}