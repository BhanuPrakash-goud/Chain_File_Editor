using ChainFileEditor.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class ModeRequiredRule : ValidationRuleBase
    {
        public override string RuleId => "R-003";
        public override string Description => "Validates that all sections have a required mode";

        public override ValidationResult Validate(ChainModel chain)
        {
            var issues = new List<ValidationIssue>();
            var validModes = new[] { "source", "binary", "ignore" };

            foreach (var section in chain.Sections)
            {
                var mode = section.Properties.GetValueOrDefault("mode", "");
                if (string.IsNullOrWhiteSpace(mode))
                {
                    issues.Add(new ValidationIssue("ModeRequired", $"'{section.Name}' is missing required mode.", ValidationSeverity.Error, section.Name, true, "Set mode to 'source'"));
                }
                else if (!validModes.Contains(mode.ToLower()))
                {
                    issues.Add(CreateError($"'{section.Name}' has invalid mode '{mode}'. Valid modes: {string.Join(", ", validModes)}.", section.Name));
                }
            }

            return issues.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(issues.ToArray());
        }
    }
}