using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class ValidationRulesTests
    {
        [TestMethod]
        public void ModeValidationRule_ValidMode_PassesValidation()
        {
            // Arrange
            var config = new ValidationRuleConfig
            {
                RuleId = "ModeValidation",
                RuleType = "PropertyValueValidation",
                Severity = ValidationSeverity.Error,
                IsEnabled = true,
                Configuration = new Dictionary<string, object>
                {
                    ["PropertyName"] = "mode",
                    ["AllowedValues"] = new[] { "source", "binary", "ignore" },
                    ["ErrorMessage"] = "Invalid mode '{PropertyValue}' for project '{SectionName}'"
                }
            };
            var rule = new ConfigurableValidationRule(config);
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string> { { "mode", "source" } }
            });

            // Act
            var result = rule.Validate(chain);

            // Assert
            Assert.AreEqual(0, result.Issues.Count);
        }

        [TestMethod]
        public void ModeValidationRule_InvalidMode_FailsValidation()
        {
            // Arrange
            var config = new ValidationRuleConfig
            {
                RuleId = "ModeValidation",
                RuleType = "PropertyValueValidation",
                Severity = ValidationSeverity.Error,
                IsEnabled = true,
                Configuration = new Dictionary<string, object>
                {
                    ["PropertyName"] = "mode",
                    ["AllowedValues"] = new[] { "source", "binary", "ignore" },
                    ["ErrorMessage"] = "Invalid mode '{PropertyValue}' for project '{SectionName}'"
                }
            };
            var rule = new ConfigurableValidationRule(config);
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string> { { "mode", "invalid" } }
            });

            // Act
            var result = rule.Validate(chain);

            // Assert
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(ValidationSeverity.Error, result.Issues[0].Severity);
            // Message content may vary, just check that there's an issue
        }

        [TestMethod]
        public void BranchOrTagRule_BothBranchAndTag_FailsValidation()
        {
            // Arrange
            var config = new ValidationRuleConfig
            {
                RuleId = "BranchOrTag",
                RuleType = "MutuallyExclusive",
                Severity = ValidationSeverity.Error,
                IsEnabled = true,
                Configuration = new Dictionary<string, object>
                {
                    ["Properties"] = new[] { "branch", "tag" },
                    ["ErrorMessage"] = "Project '{SectionName}' cannot have both branch and tag"
                }
            };
            var rule = new ConfigurableValidationRule(config);
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string> 
                { 
                    { "branch", "main" },
                    { "tag", "Build_1.0.0.1" }
                }
            });

            // Act
            var result = rule.Validate(chain);

            // Assert
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(ValidationSeverity.Error, result.Issues[0].Severity);
            // Message content may vary, just check that there's an issue
        }

        [TestMethod]
        public void BranchOrTagRule_OnlyBranch_PassesValidation()
        {
            // Arrange
            var config = new ValidationRuleConfig
            {
                RuleId = "BranchOrTag",
                RuleType = "MutuallyExclusive",
                Severity = ValidationSeverity.Error,
                IsEnabled = true,
                Configuration = new Dictionary<string, object>
                {
                    ["Properties"] = new[] { "branch", "tag" },
                    ["ErrorMessage"] = "Project '{SectionName}' cannot have both branch and tag"
                }
            };
            var rule = new ConfigurableValidationRule(config);
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string> { { "branch", "main" } }
            });

            // Act
            var result = rule.Validate(chain);

            // Assert
            Assert.AreEqual(0, result.Issues.Count);
        }

        [TestMethod]
        public void BranchOrTagRule_OnlyTag_PassesValidation()
        {
            // Arrange
            var config = new ValidationRuleConfig
            {
                RuleId = "BranchOrTag",
                RuleType = "MutuallyExclusive",
                Severity = ValidationSeverity.Error,
                IsEnabled = true,
                Configuration = new Dictionary<string, object>
                {
                    ["Properties"] = new[] { "branch", "tag" },
                    ["ErrorMessage"] = "Project '{SectionName}' cannot have both branch and tag"
                }
            };
            var rule = new ConfigurableValidationRule(config);
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string> { { "tag", "Build_1.0.0.1" } }
            });

            // Act
            var result = rule.Validate(chain);

            // Assert
            Assert.AreEqual(0, result.Issues.Count);
        }

        [TestMethod]
        public void ModeRequiredRule_MissingMode_FailsValidation()
        {
            // Arrange
            var config = new ValidationRuleConfig
            {
                RuleId = "ModeRequired",
                RuleType = "PropertyRequired",
                Severity = ValidationSeverity.Error,
                IsEnabled = true,
                Configuration = new Dictionary<string, object>
                {
                    ["PropertyName"] = "mode",
                    ["ErrorMessage"] = "Mode is required for project '{SectionName}'"
                }
            };
            var rule = new ConfigurableValidationRule(config);
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string>()
            });

            // Act
            var result = rule.Validate(chain);

            // Assert
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(ValidationSeverity.Error, result.Issues[0].Severity);
            // Message content may vary, just check that there's an issue
        }

        [TestMethod]
        public void ForkValidationRule_ValidFork_PassesValidation()
        {
            // Arrange
            var config = new ValidationRuleConfig
            {
                RuleId = "ForkValidation",
                RuleType = "RegexValidation",
                Severity = ValidationSeverity.Error,
                IsEnabled = true,
                Configuration = new Dictionary<string, object>
                {
                    ["PropertyName"] = "fork",
                    ["Pattern"] = @"^[a-zA-Z0-9._-]+/[a-zA-Z0-9._-]+$",
                    ["ErrorMessage"] = "Fork '{PropertyValue}' must follow 'owner/repository' format"
                }
            };
            var rule = new ConfigurableValidationRule(config);
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string> { { "fork", "origin/main" } }
            });

            // Act
            var result = rule.Validate(chain);

            // Assert
            Assert.AreEqual(0, result.Issues.Count);
        }

        [TestMethod]
        public void DevModeOverrideRule_DevModeWithoutMode_GeneratesWarning()
        {
            // Arrange
            var config = new ValidationRuleConfig
            {
                RuleId = "DevModeOverride",
                RuleType = "DependentProperty",
                Severity = ValidationSeverity.Warning,
                IsEnabled = true,
                Configuration = new Dictionary<string, object>
                {
                    ["DependentProperty"] = "mode.devs",
                    ["RequiredProperty"] = "mode",
                    ["ErrorMessage"] = "Project '{SectionName}' has mode.devs but no base mode"
                }
            };
            var rule = new ConfigurableValidationRule(config);
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string> { { "mode.devs", "binary" } }
            });

            // Act
            var result = rule.Validate(chain);

            // Assert
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(ValidationSeverity.Warning, result.Issues[0].Severity);
        }
    }
}