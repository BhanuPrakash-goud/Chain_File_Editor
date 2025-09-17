using ChainFileEditor.Core.Models;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class GlobalVersionWhenBinaryRule : ValidationRuleBase
    {
        public override string RuleId => "R-004";
        public override string Description => "Global version.binary required when using binary mode";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var hasBinaryMode = chain.Sections.Any(s => s.Mode.ToLower() == "binary");
            
            if (hasBinaryMode)
            {
                if (string.IsNullOrWhiteSpace(chain.Global.DevsVersion))
                {
                    result.AddIssue(new ValidationIssue("GlobalVersionWhenBinary", "Global version.binary is required when any section uses binary mode.", ValidationSeverity.Error, null, true, "Set global.version.binary to '20013'"));
                }
            }
            
            return result;
        }
    }
}