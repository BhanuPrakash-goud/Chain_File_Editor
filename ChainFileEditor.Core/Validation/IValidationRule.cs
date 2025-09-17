using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Configuration;
using System.Collections.Generic;

namespace ChainFileEditor.Core.Validation
{
    public interface IValidationRule
    {
        string RuleId { get; }
        string Description { get; }
        bool IsEnabled { get; set; }
        ValidationResult Validate(ChainModel chain);
    }

    public abstract class ValidationRuleBase : IValidationRule
    {
        protected ValidationConfiguration Config { get; }

        protected ValidationRuleBase()
        {
            Config = ConfigurationLoader.LoadValidationConfig();
        }

        public abstract string RuleId { get; }
        public abstract string Description { get; }
        public bool IsEnabled { get; set; } = true;

        public abstract ValidationResult Validate(ChainModel chain);

        protected ValidationIssue CreateError(string message, string sectionName = null)
        {
            return new ValidationIssue(RuleId, message, ValidationSeverity.Error, sectionName);
        }

        protected ValidationIssue CreateWarning(string message, string sectionName = null)
        {
            return new ValidationIssue(RuleId, message, ValidationSeverity.Warning, sectionName);
        }
    }
}