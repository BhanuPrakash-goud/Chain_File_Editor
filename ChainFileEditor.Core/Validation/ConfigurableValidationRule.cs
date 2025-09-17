using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChainFileEditor.Core.Validation
{
    public class ConfigurableValidationRule : IValidationRule
    {
        private readonly ValidationRuleConfig _config;

        public string RuleId => _config.RuleId;
        public string Description => _config.Description;
        public bool IsEnabled { get; set; }

        public ConfigurableValidationRule(ValidationRuleConfig config)
        {
            _config = config;
            IsEnabled = config.IsEnabled;
        }

        public ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();

            try
            {
                switch (_config.RuleType)
                {
                    case "PropertyRequired":
                        ValidatePropertyRequired(chain, result);
                        break;
                    case "PropertyValueValidation":
                        ValidatePropertyValue(chain, result);
                        break;
                    case "MutuallyExclusive":
                        ValidateMutuallyExclusive(chain, result);
                        break;
                    case "RequiredOneOf":
                        ValidateRequiredOneOf(chain, result);
                        break;
                    case "RequiredSections":
                        ValidateRequiredSections(chain, result);
                        break;
                    case "RegexValidation":
                        ValidateRegex(chain, result);
                        break;
                    case "ConditionalValidation":
                        ValidateConditional(chain, result);
                        break;
                    case "DependentProperty":
                        ValidateDependentProperty(chain, result);
                        break;
                    case "GlobalCondition":
                        ValidateGlobalCondition(chain, result);
                        break;
                    case "VersionRangeValidation":
                        ValidateVersionRange(chain, result);
                        break;
                    case "CommentedSectionValidation":
                        ValidateCommentedSections(chain, result);
                        break;
                    case "GitValidation":
                        ValidateGitRepository(chain, result);
                        break;
                    case "VersionConsistencyValidation":
                        ValidateVersionConsistency(chain, result);
                        break;
                }
            }
            catch (Exception ex)
            {
                result.AddIssue(new ValidationIssue(RuleId, "System", ValidationSeverity.Error, $"Rule execution error: {ex.Message}"));
            }

            return result;
        }

        private void ValidatePropertyRequired(ChainModel chain, ValidationResult result)
        {
            var propertyName = GetConfigValue("PropertyName");
            var errorMessage = GetConfigValue("ErrorMessage");

            foreach (var section in chain.Sections)
            {
                if (!section.Properties.ContainsKey(propertyName) || 
                    string.IsNullOrWhiteSpace(section.Properties[propertyName]))
                {
                    result.AddIssue(new ValidationIssue(RuleId, errorMessage.Replace("{SectionName}", section.Name), _config.Severity, section.Name, true, $"Set {propertyName} to default value"));
                }
            }
        }

        private void ValidatePropertyValue(ChainModel chain, ValidationResult result)
        {
            var propertyName = GetConfigValue("PropertyName");
            var allowedValues = GetConfigArray("AllowedValues");
            var errorMessage = GetConfigValue("ErrorMessage");

            foreach (var section in chain.Sections)
            {
                if (section.Properties.TryGetValue(propertyName, out var value) && 
                    !string.IsNullOrWhiteSpace(value) &&
                    !allowedValues.Contains(value))
                {
                    result.AddIssue(new ValidationIssue(RuleId, errorMessage
                        .Replace("{SectionName}", section.Name)
                        .Replace("{PropertyValue}", value)
                        .Replace("{AllowedValues}", string.Join(", ", allowedValues)), _config.Severity, section.Name, true, $"Set {propertyName} to {allowedValues[0]}"));
                }
            }
        }

        private void ValidateMutuallyExclusive(ChainModel chain, ValidationResult result)
        {
            var properties = GetConfigArray("Properties");
            var errorMessage = GetConfigValue("ErrorMessage");

            foreach (var section in chain.Sections)
            {
                var hasProperties = properties.Where(prop => 
                    section.Properties.ContainsKey(prop) && 
                    !string.IsNullOrWhiteSpace(section.Properties[prop])).ToList();

                if (hasProperties.Count > 1)
                {
                    result.AddIssue(new ValidationIssue(RuleId, errorMessage.Replace("{SectionName}", section.Name), _config.Severity, section.Name, true, "Remove conflicting property"));
                }
            }
        }

        private void ValidateRequiredOneOf(ChainModel chain, ValidationResult result)
        {
            var properties = GetConfigArray("Properties");
            var errorMessage = GetConfigValue("ErrorMessage");

            foreach (var section in chain.Sections)
            {
                var hasAnyProperty = properties.Any(prop => 
                    section.Properties.ContainsKey(prop) && 
                    !string.IsNullOrWhiteSpace(section.Properties[prop]));

                if (!hasAnyProperty)
                {
                    result.AddIssue(new ValidationIssue(RuleId, errorMessage.Replace("{SectionName}", section.Name), _config.Severity, section.Name, true, $"Set {properties[0]} to default value"));
                }
            }
        }

        private void ValidateRequiredSections(ChainModel chain, ValidationResult result)
        {
            var requiredSections = GetConfigArray("RequiredSections");
            var errorMessage = GetConfigValue("ErrorMessage");
            var existingSections = chain.Sections.Select(s => s.Name).ToHashSet();

            foreach (var requiredSection in requiredSections)
            {
                if (!existingSections.Contains(requiredSection))
                {
                    result.AddIssue(new ValidationIssue(
                        RuleId, 
                        errorMessage.Replace("{SectionName}", requiredSection), 
                        _config.Severity, 
                        requiredSection, 
                        true, 
                        $"Add {requiredSection} project with default configuration"
                    ));
                }
            }
        }

        private void ValidateRegex(ChainModel chain, ValidationResult result)
        {
            var propertyName = GetConfigValue("PropertyName");
            var pattern = GetConfigValue("Pattern");
            var errorMessage = GetConfigValue("ErrorMessage");
            var regex = new Regex(pattern);

            foreach (var section in chain.Sections)
            {
                if (section.Properties.TryGetValue(propertyName, out var value) && 
                    !string.IsNullOrWhiteSpace(value) &&
                    !regex.IsMatch(value))
                {
                    result.AddIssue(new ValidationIssue(RuleId, section.Name, _config.Severity, errorMessage
                        .Replace("{SectionName}", section.Name)
                        .Replace("{PropertyValue}", value)));
                }
            }
        }

        private void ValidateConditional(ChainModel chain, ValidationResult result)
        {
            var sectionName = GetConfigValue("SectionName");
            var propertyName = GetConfigValue("PropertyName");
            var errorMessage = GetConfigValue("ErrorMessage");

            var targetSection = chain.Sections.FirstOrDefault(s => s.Name == sectionName);
            if (targetSection == null) return;

            if (_config.Configuration.ContainsKey("ForbiddenValues"))
            {
                var forbiddenValues = GetConfigArray("ForbiddenValues");
                if (targetSection.Properties.TryGetValue(propertyName, out var value) &&
                    forbiddenValues.Contains(value))
                {
                    result.AddIssue(new ValidationIssue(RuleId, errorMessage, _config.Severity, sectionName, true, "Change to allowed value"));
                }
            }

            if (_config.Configuration.ContainsKey("WarningCondition"))
            {
                var condition = GetConfigValue("WarningCondition");
                if (condition == "HasValue" && 
                    targetSection.Properties.ContainsKey(propertyName) &&
                    !string.IsNullOrWhiteSpace(targetSection.Properties[propertyName]))
                {
                    result.AddIssue(new ValidationIssue(RuleId, errorMessage, _config.Severity, sectionName, true, "Remove property or change to branch"));
                }
            }
        }

        private void ValidateDependentProperty(ChainModel chain, ValidationResult result)
        {
            var dependentProperty = GetConfigValue("DependentProperty");
            var requiredProperty = GetConfigValue("RequiredProperty");
            var errorMessage = GetConfigValue("ErrorMessage");

            foreach (var section in chain.Sections)
            {
                if (section.Properties.ContainsKey(dependentProperty) &&
                    !string.IsNullOrWhiteSpace(section.Properties[dependentProperty]) &&
                    (!section.Properties.ContainsKey(requiredProperty) ||
                     string.IsNullOrWhiteSpace(section.Properties[requiredProperty])))
                {
                    result.AddIssue(new ValidationIssue(RuleId, errorMessage.Replace("{SectionName}", section.Name), _config.Severity, section.Name, true, $"Add {requiredProperty} property"));
                }
            }
        }

        private void ValidateVersionRange(ChainModel chain, ValidationResult result)
        {
            var minVersion = int.Parse(GetConfigValue("MinVersion"));
            var maxVersion = int.Parse(GetConfigValue("MaxVersion"));
            var errorMessage = GetConfigValue("ErrorMessage")
                .Replace("{MinVersion}", minVersion.ToString())
                .Replace("{MaxVersion}", maxVersion.ToString());

            // Check global version
            if (!string.IsNullOrWhiteSpace(chain.Global.VersionBinary))
            {
                if (int.TryParse(chain.Global.VersionBinary, out var globalVersion))
                {
                    if (globalVersion < minVersion || globalVersion > maxVersion)
                    {
                        result.AddIssue(new ValidationIssue(RuleId, "global", _config.Severity, 
                            errorMessage.Replace("{VersionValue}", globalVersion.ToString())));
                    }
                }
            }

            // Check project versions in tags
            foreach (var section in chain.Sections)
            {
                if (section.Properties.TryGetValue("tag", out var tag) && !string.IsNullOrWhiteSpace(tag))
                {
                    // Extract version from tag (e.g., "v20013" -> "20013")
                    var versionStr = tag.TrimStart('v', 'V');
                    if (int.TryParse(versionStr, out var version))
                    {
                        if (version < minVersion || version > maxVersion)
                        {
                            result.AddIssue(new ValidationIssue(RuleId, section.Name, _config.Severity,
                                errorMessage.Replace("{VersionValue}", version.ToString())));
                        }
                    }
                }
            }
        }

        private void ValidateGlobalCondition(ChainModel chain, ValidationResult result)
        {
            var condition = GetConfigValue("Condition");
            var errorMessage = GetConfigValue("ErrorMessage");

            if (condition == "AnyProjectHasPropertyValue")
            {
                var propertyName = GetConfigValue("PropertyName");
                var propertyValue = GetConfigValue("PropertyValue");
                var requiredGlobalProperty = GetConfigValue("RequiredGlobalProperty");

                var hasCondition = chain.Sections.Any(s => 
                    s.Properties.TryGetValue(propertyName, out var value) && 
                    value == propertyValue);

                if (hasCondition)
                {
                    var globalPropertyName = requiredGlobalProperty.Replace("global.", "");
                    var hasGlobalProperty = !string.IsNullOrWhiteSpace(
                        globalPropertyName == "version.binary" ? chain.Global.VersionBinary :
                        globalPropertyName == "devs.version.binary" ? chain.Global.DevVersionBinary :
                        null);

                    if (!hasGlobalProperty)
                    {
                        result.AddIssue(new ValidationIssue(RuleId, errorMessage, _config.Severity, "Global", true, "Set global version to default value"));
                    }
                }
            }
        }

        private string GetConfigValue(string key)
        {
            return _config.Configuration.TryGetValue(key, out var value) ? value.ToString() : "";
        }

        private void ValidateCommentedSections(ChainModel chain, ValidationResult result)
        {
            var errorMessage = GetConfigValue("ErrorMessage");
            
            foreach (var section in chain.Sections.Where(s => s.IsCommented))
            {
                result.AddIssue(new ValidationIssue(RuleId, section.Name, _config.Severity,
                    errorMessage.Replace("{SectionName}", section.Name)));
            }
        }

        private void ValidateGitRepository(ChainModel chain, ValidationResult result)
        {
            var errorMessage = GetConfigValue("ErrorMessage");
            
            foreach (var section in chain.Sections)
            {
                if (section.Properties.TryGetValue("fork", out var fork) && !string.IsNullOrWhiteSpace(fork))
                {
                    if (fork.Contains("invalid") || fork.Contains("missing"))
                    {
                        result.AddIssue(new ValidationIssue(RuleId, section.Name, _config.Severity,
                            errorMessage.Replace("{SectionName}", section.Name)));
                    }
                }
            }
        }

        private void ValidateVersionConsistency(ChainModel chain, ValidationResult result)
        {
            var errorMessage = GetConfigValue("ErrorMessage");
            var versions = new Dictionary<string, List<string>>();
            
            foreach (var section in chain.Sections)
            {
                if (section.Properties.TryGetValue("tag", out var tag) && !string.IsNullOrWhiteSpace(tag))
                {
                    var versionStr = tag.TrimStart('v', 'V');
                    if (!versions.ContainsKey(versionStr))
                        versions[versionStr] = new List<string>();
                    versions[versionStr].Add(section.Name);
                }
            }
            
            if (versions.Count > 3)
            {
                result.AddIssue(new ValidationIssue(RuleId, "global", _config.Severity, errorMessage));
            }
        }

        private string[] GetConfigArray(string key)
        {
            if (_config.Configuration.TryGetValue(key, out var value))
            {
                if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray().Select(e => e.GetString()).ToArray();
                }
                if (value is string[] stringArray)
                {
                    return stringArray;
                }
                if (value is object[] objectArray)
                {
                    return objectArray.Select(o => o.ToString()).ToArray();
                }
            }
            return new string[0];
        }
    }

}