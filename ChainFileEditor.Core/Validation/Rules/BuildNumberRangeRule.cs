using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public class BuildNumberRangeRule : ValidationRuleBase
    {
        public override string RuleId => "BuildNumberRange";
        public override string Description => "Validates build numbers are within correct ranges for branch types";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var config = ConfigurationLoader.LoadValidationConfig();

            // Check global version binary
            if (!string.IsNullOrEmpty(chain.Global.VersionBinary))
            {
                ValidateBuildNumber(result, chain.Global.VersionBinary, "global.version.binary", "global");
            }

            // Check global devs version binary
            if (!string.IsNullOrEmpty(chain.Global.DevVersionBinary))
            {
                ValidateBuildNumber(result, chain.Global.DevVersionBinary, "global.devs.version.binary", "global");
            }

            return result;
        }

        private void ValidateBuildNumber(ValidationResult result, string version, string propertyName, string sectionName)
        {
            if (!int.TryParse(version, out var buildNumber))
            {
                result.AddIssue(CreateError($"Invalid build number format '{version}' for {propertyName}", sectionName));
                return;
            }

            var config = ConfigurationLoader.LoadValidationConfig();
            var ranges = config.ValidationSettings.BuildNumberRanges;

            string branchType = GetBranchType(buildNumber, ranges);
            if (string.IsNullOrEmpty(branchType))
            {
                result.AddIssue(CreateWarning($"Build number {buildNumber} for {propertyName} is outside known ranges", sectionName));
            }
        }

        private string GetBranchType(int buildNumber, Dictionary<string, BuildNumberRange> ranges)
        {
            if (ranges.ContainsKey("master") && buildNumber >= ranges["master"].Min && buildNumber <= ranges["master"].Max)
                return "master";
            if (ranges.ContainsKey("integration") && buildNumber >= ranges["integration"].Min && buildNumber <= ranges["integration"].Max)
                return "integration";
            if (ranges.ContainsKey("stage") && buildNumber >= ranges["stage"].Min && buildNumber <= ranges["stage"].Max)
                return "stage";
            if (ranges.ContainsKey("development") && buildNumber >= ranges["development"].Min && buildNumber <= ranges["development"].Max)
                return "development";
            if (ranges.ContainsKey("local") && buildNumber >= ranges["local"].Min && buildNumber <= ranges["local"].Max)
                return "local";
            
            return null;
        }
    }
}