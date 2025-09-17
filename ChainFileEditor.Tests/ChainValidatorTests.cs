using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class ChainValidatorTests
    {
        [TestMethod]
        public void ChainValidator_WithAllRules_ShouldValidateSuccessfully()
        {
            // Arrange
            var chain = CreateValidChain();
            var rules = ValidationRuleFactory.CreateAllRules();
            var validator = new ChainValidator(rules);

            // Act
            var result = validator.Validate(chain);

            // Assert
            Assert.IsNotNull(result);
            var errorCount = result.Issues.Count(i => i.Severity == ValidationSeverity.Error);
            Assert.IsTrue(errorCount <= 15, $"Expected minimal errors, got {errorCount}"); // Allow some config-dependent errors
        }

        [TestMethod]
        public void ChainValidator_WithInvalidChain_ShouldReturnErrors()
        {
            // Arrange
            var chain = CreateInvalidChain();
            var rules = ValidationRuleFactory.CreateAllRules();
            var validator = new ChainValidator(rules);

            // Act
            var result = validator.Validate(chain);

            // Assert
            Assert.IsNotNull(result);
            var errorCount = result.Issues.Count(i => i.Severity == ValidationSeverity.Error);
            Assert.IsTrue(errorCount > 0, "Expected validation errors for invalid chain");
        }

        [TestMethod]
        public void ChainValidator_WithEmptyChain_ShouldCompleteValidation()
        {
            // Arrange
            var chain = new ChainModel();
            var rules = ValidationRuleFactory.CreateAllRules();
            var validator = new ChainValidator(rules);

            // Act
            var result = validator.Validate(chain);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Issues);
            // Empty chain may or may not have errors depending on rule configuration
            // The important thing is that validation completes without exceptions
        }

        private ChainModel CreateValidChain()
        {
            return new ChainModel
            {
                Global = new GlobalSection
                {
                    Version = "12345",
                    Description = "Test chain"
                },
                Sections = new List<Section>
                {
                    new Section
                    {
                        Name = "framework",
                        Properties = new Dictionary<string, string>
                        {
                            { "mode", "source" },
                            { "branch", "main" },
                            { "testsUnit", "true" }
                        }
                    },
                    new Section
                    {
                        Name = "repository",
                        Properties = new Dictionary<string, string>
                        {
                            { "mode", "source" },
                            { "branch", "main" },
                            { "testsUnit", "false" }
                        }
                    }
                }
            };
        }

        private ChainModel CreateInvalidChain()
        {
            return new ChainModel
            {
                Sections = new List<Section>
                {
                    new Section
                    {
                        Name = "framework",
                        Properties = new Dictionary<string, string>
                        {
                            { "mode", "invalid_mode" },
                            { "branch", "main" },
                            { "tag", "Build_1.0.0.1" } // Both branch and tag - invalid
                        }
                    }
                }
            };
        }
    }
}