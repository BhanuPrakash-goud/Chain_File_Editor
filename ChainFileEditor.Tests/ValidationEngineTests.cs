using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Validation.Rules;
using ChainFileEditor.Core.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class ValidationEngineTests
    {
        [TestMethod]
        public void ValidationEngine_ShouldExecuteAllEnabledRules()
        {
            // Arrange
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string> { { "mode", "invalid" } }
            });

            var rules = new List<IValidationRule> { new ModeValidationRule() };
            var validator = new ChainValidator(rules);

            // Act
            var result = validator.Validate(chain);

            // Assert
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual("ModeValidation", result.Issues[0].RuleId);
        }

        [TestMethod]
        public void BranchOrTagRule_ShouldFailWhenBothPresent()
        {
            // Arrange
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

            var rule = new BranchOrTagRule();

            // Act
            var result = rule.Validate(chain);

            // Assert
            Assert.AreEqual(1, result.Issues.Count);
            Assert.IsTrue(result.Issues[0].Message.Contains("cannot have both branch and tag"));
        }

        [TestMethod]
        public void ModeValidationRule_ShouldPassWithValidMode()
        {
            // Arrange
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string> { { "mode", "source" } }
            });

            var rule = new ModeValidationRule();

            // Act
            var result = rule.Validate(chain);

            // Assert
            Assert.AreEqual(0, result.Issues.Count);
        }
    }
}