using ChainFileEditor.Core.Models;

namespace ChainFileEditor.Core.Validation.Rules
{
    public class BranchOrTagRequiredRule : ValidationRuleBase
    {
        public override string RuleId => "BranchOrTagRequired";
        public override string Description => "Every project must have either branch or tag";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();

            foreach (var section in chain.Sections)
            {
                var hasBranch = section.Properties.ContainsKey("branch") && !string.IsNullOrWhiteSpace(section.Properties["branch"]);
                var hasTag = section.Properties.ContainsKey("tag") && !string.IsNullOrWhiteSpace(section.Properties["tag"]);

                if (!hasBranch && !hasTag)
                {
                    result.AddIssue(new ValidationIssue("BranchOrTagRequired", $"Project '{section.Name}' must have either branch or tag", ValidationSeverity.Error, section.Name, true, "Set branch to 'integration'"));
                }
            }

            return result;
        }
    }
}