using ChainFileEditor.Core.Models;
using System;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public sealed class GitRepositoryValidationRule : ValidationRuleBase
    {
        public override string RuleId => "R-013";
        public override string Description => "Validates git repository and fork existence";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var knownRepositories = Config.MainRepositories;
            
            foreach (var section in chain.Sections)
            {
                var fork = section.Fork;
                
                if (!string.IsNullOrWhiteSpace(fork))
                {
                    var parts = fork.Split('/');
                    if (parts.Length != 2)
                    {
                        result.AddIssue(CreateError($"Fork '{fork}' in project '{section.Name}' should follow format 'username/repository'", section.Name));
                    }
                }
                
                // Validate repository exists in known repositories
                if (!knownRepositories.Contains(section.Name, StringComparer.OrdinalIgnoreCase))
                {
                    result.AddIssue(CreateWarning($"'{section.Name}' is not a known main repository", section.Name));
                }
            }
            
            return result;
        }
    }
}