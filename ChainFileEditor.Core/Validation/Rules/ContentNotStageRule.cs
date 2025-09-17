using ChainFileEditor.Core.Models;
using System;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class ContentNotStageRule : ValidationRuleBase
    {
        public override string RuleId => "R-005";
        public override string Description => "Content project cannot use stage branch";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var restrictedProjects = new[] { "content" };
            var restrictedBranches = new[] { "stage" };
            
            foreach (var section in chain.Sections)
            {
                var branch = section.Branch;
                if (restrictedProjects.Contains(section.Name, StringComparer.OrdinalIgnoreCase) && 
                    !string.IsNullOrEmpty(branch) && 
                    restrictedBranches.Contains(branch, StringComparer.OrdinalIgnoreCase))
                {
                    result.AddIssue(new ValidationIssue("ContentNotStage", $"Project '{section.Name}' cannot use '{branch}' branch - use 'integration' instead.", ValidationSeverity.Error, section.Name, true, "Change branch to 'integration'"));
                }
            }
            
            return result;
        }
    }
}