using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Validation.Rules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ChainFileEditor.Core.Configuration
{
    public static class ValidationRuleFactory
    {
        private static List<ValidationRuleConfig> _ruleConfigs;
        private static readonly object _lock = new object();

        public static List<IValidationRule> CreateAllRules()
        {
            LoadRuleConfigurations();
            
            return _ruleConfigs
                .Where(config => config.IsEnabled)
                .Select(config => new ConfigurableValidationRule(config))
                .Cast<IValidationRule>()
                .ToList();
        }

        public static IValidationRule? CreateRule(string ruleId)
        {
            LoadRuleConfigurations();
            
            var config = _ruleConfigs.FirstOrDefault(c => c.RuleId == ruleId);
            return config != null ? new ConfigurableValidationRule(config) : null;
        }

        private static void LoadRuleConfigurations()
        {
            if (_ruleConfigs != null) return;

            lock (_lock)
            {
                if (_ruleConfigs != null) return;

                try
                {
                    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "ValidationRules.json");
                    
                    if (!File.Exists(configPath))
                    {
                        _ruleConfigs = CreateDefaultRuleConfigurations();
                        return;
                    }

                    var json = File.ReadAllText(configPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                    };
                    
                    var rulesContainer = JsonSerializer.Deserialize<ValidationRulesContainer>(json, options);
                    _ruleConfigs = rulesContainer?.ValidationRules ?? CreateDefaultRuleConfigurations();
                }
                catch (Exception)
                {
                    _ruleConfigs = CreateDefaultRuleConfigurations();
                }
            }
        }

        private static List<ValidationRuleConfig> CreateDefaultRuleConfigurations()
        {
            return new List<ValidationRuleConfig>
            {
                new ValidationRuleConfig
                {
                    RuleId = "ModeRequired",
                    Name = "Mode Required Rule",
                    Description = "Every project must have a mode specified",
                    IsEnabled = true,
                    Severity = ValidationSeverity.Error,
                    RuleType = "PropertyRequired",
                    Configuration = new Dictionary<string, object>
                    {
                        ["PropertyName"] = "mode",
                        ["ErrorMessage"] = "Mode is required for project '{SectionName}'"
                    }
                },
                new ValidationRuleConfig
                {
                    RuleId = "ModeValidation",
                    Name = "Mode Validation Rule",
                    Description = "Mode must be one of the allowed values",
                    IsEnabled = true,
                    Severity = ValidationSeverity.Error,
                    RuleType = "PropertyValueValidation",
                    Configuration = new Dictionary<string, object>
                    {
                        ["PropertyName"] = "mode",
                        ["AllowedValues"] = new[] { "source", "binary", "ignore" },
                        ["ErrorMessage"] = "Invalid mode '{PropertyValue}' for project '{SectionName}'. Allowed values: {AllowedValues}"
                    }
                },
                new ValidationRuleConfig
                {
                    RuleId = "BranchOrTag",
                    Name = "Branch Or Tag Rule",
                    Description = "Project cannot have both branch and tag specified",
                    IsEnabled = true,
                    Severity = ValidationSeverity.Error,
                    RuleType = "MutuallyExclusive",
                    Configuration = new Dictionary<string, object>
                    {
                        ["Properties"] = new[] { "branch", "tag" },
                        ["ErrorMessage"] = "Project '{SectionName}' cannot have both branch and tag specified"
                    }
                },
                new ValidationRuleConfig
                {
                    RuleId = "BranchOrTagRequired",
                    Name = "Branch Or Tag Required Rule",
                    Description = "Every project must have either branch or tag",
                    IsEnabled = true,
                    Severity = ValidationSeverity.Error,
                    RuleType = "RequiredOneOf",
                    Configuration = new Dictionary<string, object>
                    {
                        ["Properties"] = new[] { "branch", "tag" },
                        ["ErrorMessage"] = "Project '{SectionName}' must have either branch or tag"
                    }
                },
                new ValidationRuleConfig
                {
                    RuleId = "RequiredProjects",
                    Name = "Required Projects Rule",
                    Description = "Essential projects must be present in chain",
                    IsEnabled = true,
                    Severity = ValidationSeverity.Error,
                    RuleType = "RequiredSections",
                    Configuration = new Dictionary<string, object>
                    {
                        ["RequiredSections"] = new[] { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment" },
                        ["ErrorMessage"] = "Required project '{SectionName}' is missing from chain"
                    }
                },
                new ValidationRuleConfig
                {
                    RuleId = "BuildNumberRange",
                    Name = "Build Number Range Rule",
                    Description = "Build numbers must be within valid ranges for branch types",
                    IsEnabled = true,
                    Severity = ValidationSeverity.Warning,
                    RuleType = "BuildNumberRange",
                    Configuration = new Dictionary<string, object>
                    {
                        ["ErrorMessage"] = "Build number {PropertyValue} is outside known ranges for {PropertyName}"
                    }
                },
                new ValidationRuleConfig
                {
                    RuleId = "IntegrationTests",
                    Name = "Integration Tests Rule",
                    Description = "Integration test suites must be valid",
                    IsEnabled = true,
                    Severity = ValidationSeverity.Warning,
                    RuleType = "IntegrationTests",
                    Configuration = new Dictionary<string, object>
                    {
                        ["ErrorMessage"] = "Unknown integration test suite '{PropertyValue}'"
                    }
                },
                new ValidationRuleConfig
                {
                    RuleId = "GlobalDevVersionRequired",
                    Name = "Global Dev Version Required Rule",
                    Description = "Global dev version binary must be set when any project uses mode.devs = binary",
                    IsEnabled = true,
                    Severity = ValidationSeverity.Warning,
                    RuleType = "GlobalDevVersionRequired",
                    Configuration = new Dictionary<string, object>
                    {
                        ["ErrorMessage"] = "global.devs.version.binary is required when projects use mode.devs = binary"
                    }
                }
            };
        }

        public static void ReloadRules()
        {
            lock (_lock)
            {
                _ruleConfigs = null;
            }
        }
    }

    public class ValidationRulesContainer
    {
        public List<ValidationRuleConfig> ValidationRules { get; set; }
    }
}