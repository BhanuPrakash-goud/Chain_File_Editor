using ChainFileEditor.Core.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class ForkValidationRule : ValidationRuleBase
    {
        public override string RuleId => "R-009";
        public override string Description => "Validates fork format and requirements";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var forkPattern = new Regex(@"^[a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+$");
            
            foreach (var section in chain.Sections)
            {
                var fork = section.Fork;
                var branch = section.Branch;
                
                if (!string.IsNullOrWhiteSpace(fork))
                {
                    if (!forkPattern.IsMatch(fork))
                    {
                        result.AddIssue(CreateError($"Fork '{fork}' in project '{section.Name}' should follow format 'username/repository'.", section.Name));
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(branch) && 
                    branch.StartsWith("dev/") && 
                    string.IsNullOrWhiteSpace(fork))
                {
                    result.AddIssue(CreateWarning($"Development branch '{branch}' in project '{section.Name}' should have a fork specified.", section.Name));
                }
            }
            
            return result;
        }
    }
}