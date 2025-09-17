using System.Collections.Generic;

namespace ChainFileEditor.Core.Validation
{
    public enum ValidationSeverity
    {
        Error,
        Warning,
        Info
    }

    public class ValidationIssue
    {
        public string RuleId { get; }
        public string Message { get; }
        public ValidationSeverity Severity { get; }
        public string SectionName { get; }
        public bool IsAutoFixable { get; }
        public string SuggestedFix { get; }

        public ValidationIssue(string ruleId, string message, ValidationSeverity severity, string sectionName = null, bool isAutoFixable = false, string suggestedFix = null)
        {
            RuleId = ruleId;
            Message = message;
            Severity = severity;
            SectionName = sectionName;
            IsAutoFixable = isAutoFixable;
            SuggestedFix = suggestedFix;
        }
    }

    public class ValidationResult
    {
        public bool IsValid => Issues.Count == 0;
        public List<ValidationIssue> Issues { get; }

        public ValidationResult()
        {
            Issues = new List<ValidationIssue>();
        }

        public ValidationResult(List<ValidationIssue> issues)
        {
            Issues = issues ?? new List<ValidationIssue>();
        }

        public void AddIssue(ValidationIssue issue)
        {
            Issues.Add(issue);
        }

        public ValidationResult Merge(ValidationResult other)
        {
            if (other != null)
            {
                foreach (var issue in other.Issues)
                {
                    Issues.Add(issue);
                }
            }
            return this;
        }

        public static ValidationResult Success() => new ValidationResult();
        public static ValidationResult Failure(params ValidationIssue[] issues) => new ValidationResult(new List<ValidationIssue>(issues));
    }

    public class ValidationReport
    {
        public bool IsValid { get; }
        public List<ValidationIssue> Issues { get; }

        public ValidationReport(List<ValidationIssue> issues)
        {
            Issues = issues ?? new List<ValidationIssue>();
            IsValid = Issues.Count == 0;
        }
    }
}