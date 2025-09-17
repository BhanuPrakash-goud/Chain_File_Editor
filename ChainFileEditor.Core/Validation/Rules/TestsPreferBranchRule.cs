using ChainFileEditor.Core.Models;
using System;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class TestsPreferBranchRule : ValidationRuleBase
    {
        public override string RuleId => "R-006";
        public override string Description => "Tests project should prefer branches over tags";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var preferredProjects = new[] { "tests" };
            
            foreach (var section in chain.Sections)
            {
                if (preferredProjects.Contains(section.Name, StringComparer.OrdinalIgnoreCase) && 
                    !string.IsNullOrWhiteSpace(section.Tag) && 
                    string.IsNullOrWhiteSpace(section.Branch))
                {
                    result.AddIssue(new ValidationIssue("TestsPreferBranch", $"Project '{section.Name}' should prefer branch over tag for better flexibility.", ValidationSeverity.Warning, section.Name, true, "Replace tag with 'integration' branch"));
                }
            }
            
            return result;
        }
    }
}