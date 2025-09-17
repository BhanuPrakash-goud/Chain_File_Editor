using ChainFileEditor.Core.Models;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class DevModeOverrideRule : ValidationRuleBase
    {
        public override string RuleId => "R-012";
        public override string Description => "Detects dev mode overrides";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            
            foreach (var section in chain.Sections)
            {
                var mode = section.Mode;
                var devMode = section.DevMode;
                
                if (!string.IsNullOrEmpty(devMode) && string.IsNullOrEmpty(mode))
                {
                    result.AddIssue(new ValidationIssue("DevModeOverride", $"Project '{section.Name}' has dev mode '{devMode}' but no base mode specified.", ValidationSeverity.Warning, section.Name, true, "Set base mode to 'source'"));
                }
                else if (!string.IsNullOrEmpty(devMode) && devMode != mode)
                {
                    result.AddIssue(CreateWarning($"Project '{section.Name}' has dev mode override: {mode} -> {devMode}", section.Name));
                }
            }
            
            return result;
        }
    }
}