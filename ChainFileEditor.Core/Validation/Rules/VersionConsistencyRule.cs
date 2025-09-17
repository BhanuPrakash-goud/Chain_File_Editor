using ChainFileEditor.Core.Models;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class VersionConsistencyRule : ValidationRuleBase
    {
        public override string RuleId => "R-008";
        public override string Description => "Validates version consistency for binary mode";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var binarySections = chain.Sections.Where(s => s.Mode.ToLower() == "binary").ToList();
            
            if (binarySections.Any() && string.IsNullOrWhiteSpace(chain.Global.DevsVersion))
            {
                result.AddIssue(CreateError("Global binary version is required when projects use binary mode.", "global"));
            }

            foreach (var section in binarySections)
            {
                var branch = section.Branch;
                if (!string.IsNullOrWhiteSpace(branch))
                {
                    result.AddIssue(CreateError($"Binary mode project '{section.Name}' should use tag, not branch.", section.Name));
                }
            }
            
            return result;
        }
    }
}