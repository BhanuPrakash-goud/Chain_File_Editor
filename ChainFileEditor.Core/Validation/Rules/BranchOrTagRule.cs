using ChainFileEditor.Core.Models;

namespace ChainFileEditor.Core.Validation.Rules
{
    public class BranchOrTagRule : ValidationRuleBase
    {
        public override string RuleId => "BranchOrTag";
        public override string Description => "Ensures projects have either branch or tag, never both";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();

            foreach (var section in chain.Sections)
            {
                var hasBranch = section.Properties.ContainsKey("branch") && !string.IsNullOrWhiteSpace(section.Properties["branch"]);
                var hasTag = section.Properties.ContainsKey("tag") && !string.IsNullOrWhiteSpace(section.Properties["tag"]);

                if (hasBranch && hasTag)
                {
                    result.AddIssue(new ValidationIssue("BranchOrTag", $"Project '{section.Name}' cannot have both branch and tag - choose one", ValidationSeverity.Error, section.Name, true, "Remove tag and keep branch"));
                }
            }

            return result;
        }
    }
}