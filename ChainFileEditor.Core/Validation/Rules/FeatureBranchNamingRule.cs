using ChainFileEditor.Core.Models;
using System.Text.RegularExpressions;

namespace ChainFileEditor.Core.Validation.Rules
{
    public class FeatureBranchNamingRule : ValidationRuleBase
    {
        private readonly Regex _featureBranchPattern = new Regex(@"^dev/DEPM-\d+", RegexOptions.IgnoreCase);

        public override string RuleId => "FeatureBranchNaming";
        public override string Description => "Feature branches must follow naming convention dev/DEPM-{number}";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();

            foreach (var section in chain.Sections)
            {
                if (!string.IsNullOrEmpty(section.Branch) && section.Branch.StartsWith("dev/"))
                {
                    if (!_featureBranchPattern.IsMatch(section.Branch))
                    {
                        result.AddIssue(CreateWarning($"Feature branch '{section.Branch}' should follow naming convention 'dev/DEPM-{{number}}-{{description}}'", section.Name));
                    }
                }
            }

            return result;
        }
    }
}