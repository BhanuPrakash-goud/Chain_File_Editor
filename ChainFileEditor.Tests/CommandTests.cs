using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        private string _testFilePath = null!;
        private ChainFileParser _parser = null!;
        private ChainFileWriter _writer = null!;
        private ModeService _modeService = null!;
        private BranchService _branchService = null!;

        [TestInitialize]
        public void Setup()
        {
            _testFilePath = Path.GetTempFileName();
            _parser = new ChainFileParser();
            _writer = new ChainFileWriter();
            _modeService = new ModeService();
            _branchService = new BranchService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
        }

        [TestMethod]
        public void FullWorkflow_ParseModifyWrite_WorksCorrectly()
        {
            // Arrange
            var content = @"global.version.binary=1.0.0
framework.mode=source
framework.branch=main
app.mode=binary";
            File.WriteAllText(_testFilePath, content);

            // Act - Parse
            var chain = _parser.ParsePropertiesFile(_testFilePath);
            
            // Debug: Check initial state
            Assert.AreEqual(2, chain.Sections.Count, "Should have 2 sections after parsing");
            Assert.IsTrue(chain.Sections.Any(s => s.Name == "framework"), "Should have framework section");
            Assert.IsTrue(chain.Sections.Any(s => s.Name == "app"), "Should have app section");
            
            // Act - Modify modes
            var modeResult = _modeService.SetMode(chain, "framework", "binary");
            Assert.IsTrue(modeResult, "SetMode should return true");
            
            // Act - Update branches
            var branchUpdates = new Dictionary<string, string> { { "app", "develop" } };
            var branchResult = _branchService.UpdateProjectBranches(chain, branchUpdates);
            Assert.AreEqual(1, branchResult, "Should update 1 branch");
            
            // Act - Write back
            _writer.WritePropertiesFile(_testFilePath, chain);

            // Assert
            var updatedChain = _parser.ParsePropertiesFile(_testFilePath);
            Assert.AreEqual("1.0.0", updatedChain.Global.VersionBinary);
            Assert.AreEqual("binary", updatedChain.Sections.First(s => s.Name == "framework").Mode);
            var appSection = updatedChain.Sections.First(s => s.Name == "app");
            Assert.AreEqual("develop", appSection.Branch, "App section should have develop branch");
        }

        [TestMethod]
        public void ValidationWorkflow_DetectsAndReportsIssues()
        {
            // Arrange
            var content = @"framework.mode=invalid
framework.branch=main
framework.tag=Build_1.0.0.1";
            File.WriteAllText(_testFilePath, content);
            var chain = _parser.ParsePropertiesFile(_testFilePath);

            var modeConfig = new ValidationRuleConfig
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
            
            var branchTagConfig = new ValidationRuleConfig
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

            // Act
            var validator = new ChainValidator(new List<IValidationRule> 
            { 
                new ConfigurableValidationRule(modeConfig),
                new ConfigurableValidationRule(branchTagConfig)
            });
            var result = validator.Validate(chain);

            // Assert
            Assert.AreEqual(2, result.Issues.Count);
            Assert.IsTrue(result.Issues.Count >= 1); // At least one validation issue should be found
        }

        [TestMethod]
        public void ServiceIntegration_ModeAndBranchOperations_WorkTogether()
        {
            // Arrange
            var chain = new ChainModel();
            chain.Sections.Add(new Section 
            { 
                Name = "framework",
                Properties = new Dictionary<string, string> { { "mode", "source" } }
            });

            // Act
            var modeUpdated = _modeService.SetMode(chain, "framework", "binary", "source");
            var branchUpdated = _branchService.UpdateProjectBranches(chain, 
                new Dictionary<string, string> { { "framework", "feature/test" } });

            // Assert
            Assert.IsTrue(modeUpdated);
            Assert.AreEqual(1, branchUpdated);
            
            var section = chain.Sections.First(s => s.Name == "framework");
            Assert.AreEqual("binary", section.Mode);
            Assert.AreEqual("source", section.DevMode);
            Assert.AreEqual("feature/test", section.Branch);
        }
    }
}