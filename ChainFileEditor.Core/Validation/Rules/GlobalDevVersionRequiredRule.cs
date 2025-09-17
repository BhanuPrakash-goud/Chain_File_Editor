using ChainFileEditor.Core.Models;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public class GlobalDevVersionRequiredRule : ValidationRuleBase
    {
        public override string RuleId => "GlobalDevVersionRequired";
        public override string Description => "Global dev version binary must be set when any project uses mode.devs = binary";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();

            if (chain?.Sections == null) return result;

            var hasDevsBinary = chain.Sections.Any(s => 
                s.Properties != null && s.Properties.ContainsKey("mode.devs") && s.Properties["mode.devs"] == "binary");

            if (hasDevsBinary)
            {
                var hasGlobalDevVersion = !string.IsNullOrEmpty(chain.Global?.DevVersionBinary);
                
                if (!hasGlobalDevVersion)
                {
                    result.AddIssue(new ValidationIssue(
                        RuleId,
                        "global.devs.version.binary is required when projects use mode.devs = binary",
                        ValidationSeverity.Warning,
                        "global"
                    ));
                }
            }

            return result;
        }
    }
}