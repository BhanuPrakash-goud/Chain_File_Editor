using ChainFileEditor.Core.Models;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public class ModeValidationRule : ValidationRuleBase
    {
        private readonly string[] _validModes = { "source", "binary", "ignore" };

        public override string RuleId => "ModeValidation";
        public override string Description => "Validates project mode values";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();

            foreach (var section in chain.Sections)
            {
                if (section.Properties.TryGetValue("mode", out var mode))
                {
                    if (!_validModes.Contains(mode))
                    {
                        result.AddIssue(new ValidationIssue("ModeValidation", $"Invalid mode '{mode}'. Valid modes: {string.Join(", ", _validModes)}", ValidationSeverity.Error, section.Name, true, "Set mode to 'source'"));
                    }
                }
                else
                {
                    result.AddIssue(new ValidationIssue("ModeValidation", "Mode is required for all projects", ValidationSeverity.Error, section.Name, true, "Set mode to 'source'"));
                }
            }

            return result;
        }
    }
}