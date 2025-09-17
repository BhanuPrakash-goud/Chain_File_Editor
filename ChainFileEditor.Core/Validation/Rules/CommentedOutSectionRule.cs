using ChainFileEditor.Core.Models;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class CommentedOutSectionRule : ValidationRuleBase
    {
        public override string RuleId => "R-010";
        public override string Description => "Detects commented out configuration sections";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            // This rule would need access to raw content which isn't available in ChainModel
            // Skipping implementation for now
            return result;
        }
    }
}