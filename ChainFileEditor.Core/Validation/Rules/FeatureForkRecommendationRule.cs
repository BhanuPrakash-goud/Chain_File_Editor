using ChainFileEditor.Core.Models;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class FeatureForkRecommendationRule : ValidationRuleBase
    {
        public override string RuleId => "R-002";
        public override string Description => "Feature branches should have forks for collaboration";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var featurePattern = "dev/DEPM-";
            
            foreach (var section in chain.Sections)
            {
                var branch = section.Branch;
                var fork = section.Fork;
                
                if (!string.IsNullOrWhiteSpace(branch) && 
                    branch.StartsWith(featurePattern) && 
                    string.IsNullOrWhiteSpace(fork))
                {
                    result.AddIssue(CreateWarning($"Feature branch '{branch}' in '{section.Name}' should have a fork specified for collaboration.", section.Name));
                }
            }
            
            return result;
        }
    }
}