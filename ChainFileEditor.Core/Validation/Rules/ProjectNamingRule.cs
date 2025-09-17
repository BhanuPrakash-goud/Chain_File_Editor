using ChainFileEditor.Core.Models;
using System.Text.RegularExpressions;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class ProjectNamingRule : ValidationRuleBase
    {
        public override string RuleId => "R-007";
        public override string Description => "Validates project naming conventions";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var pattern = new Regex(@"^[a-z][a-z0-9_]*$");
            
            foreach (var section in chain.Sections)
            {
                if (!pattern.IsMatch(section.Name))
                {
                    result.AddIssue(CreateError($"Project '{section.Name}' should follow naming convention (lowercase, alphanumeric, underscores).", section.Name));
                }
            }
            
            return result;
        }
    }
}