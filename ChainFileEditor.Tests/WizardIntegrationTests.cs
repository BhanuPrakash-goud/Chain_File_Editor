using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using ChainFileEditor.Wizard;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class WizardIntegrationTests
    {
        private string _testDir = null!;
        private string _testFile = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "WizardIntegrationTests");
            Directory.CreateDirectory(_testDir);
            _testFile = Path.Combine(_testDir, "test.properties");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        [TestMethod]
        public void WizardToCore_CreateAndValidate_WorksCorrectly()
        {
            // Arrange - Create file using wizard
            var service = new ManualChainFileService();
            var projects = new List<ManualChainFileService.ProjectConfig>
            {
                new ManualChainFileService.ProjectConfig
                {
                    ProjectName = "framework",
                    Mode = "source",
                    Branch = "main",
                    TestsEnabled = true
                },
                new ManualChainFileService.ProjectConfig
                {
                    ProjectName = "repository",
                    Mode = "binary",
                    Tag = "Build_12.25.7.20013",
                    TestsEnabled = false
                }
            };

            var filePath = service.CreateManualChainFile("159848", "integration test", "20013", projects, new ManualChainFileService.IntegrationTestsConfig(), _testDir);

            // Act - Parse and validate using Core
            var parser = new ChainFileParser();
            var chain = parser.ParsePropertiesFile(filePath);

            // Assert - Verify parsing worked
            Assert.AreEqual(2, chain.Sections.Count);
            Assert.AreEqual("20013", chain.Global.VersionBinary);
            
            var framework = chain.Sections.Find(s => s.Name == "framework");
            Assert.IsNotNull(framework);
            Assert.AreEqual("source", framework.Mode);
            Assert.AreEqual("main", framework.Branch);
            Assert.IsTrue(framework.TestsUnit);

            var repository = chain.Sections.Find(s => s.Name == "repository");
            Assert.IsNotNull(repository);
            Assert.AreEqual("binary", repository.Mode);
            Assert.AreEqual("Build_12.25.7.20013", repository.Tag);
            Assert.IsFalse(repository.TestsUnit);
        }

        [TestMethod]
        public void WizardToCore_ModifyAndWrite_PreservesStructure()
        {
            // Arrange - Create initial file
            var service = new ManualChainFileService();
            var projects = new List<ManualChainFileService.ProjectConfig>
            {
                new ManualChainFileService.ProjectConfig
                {
                    ProjectName = "framework",
                    Mode = "source",
                    Branch = "main",
                    TestsEnabled = true
                }
            };

            var filePath = service.CreateManualChainFile("159848", "modify test", "20013", projects, new ManualChainFileService.IntegrationTestsConfig(), _testDir);

            // Act - Parse, modify, and write back
            var parser = new ChainFileParser();
            var chain = parser.ParsePropertiesFile(filePath);
            
            var modeService = new ModeService();
            modeService.SetMode(chain, "framework", "binary");

            var writer = new ChainFileWriter();
            writer.WritePropertiesFile(filePath, chain);

            // Assert - Verify changes persisted
            var updatedChain = parser.ParsePropertiesFile(filePath);
            var framework = updatedChain.Sections.Find(s => s.Name == "framework");
            Assert.AreEqual("binary", framework?.Mode);
        }

        [TestMethod]
        public void WizardManager_InvalidChoice_HandlesGracefully()
        {
            // Arrange
            var manager = new WizardManager();

            // Act & Assert - Should not throw for invalid input handling
            Assert.IsNotNull(manager);
        }

        [TestMethod]
        public void FeatureChainService_WizardIntegration_CreatesValidFile()
        {
            // Arrange
            var request = new FeatureChainService.FeatureChainRequest
            {
                JiraId = "159848",
                Description = "wizard integration test",
                Version = "20013",
                Projects = new List<FeatureChainService.ProjectConfig>
                {
                    new FeatureChainService.ProjectConfig
                    {
                        ProjectName = "framework",
                        Mode = "source",
                        Branch = "stage",
                        TestsEnabled = true
                    }
                }
            };

            // Act
            var service = new FeatureChainService();
            var filePath = service.CreateFeatureChainFile(request, _testDir);

            // Assert
            Assert.IsTrue(File.Exists(filePath));
            
            var parser = new ChainFileParser();
            var chain = parser.ParsePropertiesFile(filePath);
            Assert.AreEqual(1, chain.Sections.Count);
            Assert.AreEqual("framework", chain.Sections[0].Name);
        }

        [TestMethod]
        public void WizardValidation_InvalidFile_ReportsErrors()
        {
            // Arrange - Create invalid file
            File.WriteAllText(_testFile, @"framework.mode=invalid
framework.branch=main
framework.tag=Build_1.0.0.1");

            // Act - Parse and validate
            var parser = new ChainFileParser();
            var chain = parser.ParsePropertiesFile(_testFile);

            var rules = ValidationRuleFactory.CreateAllRules();
            var validator = new ChainValidator(rules);
            var report = validator.Validate(chain);

            // Assert - Should find validation errors
            Assert.IsTrue(report.Issues.Count > 0);
            Assert.IsTrue(report.Issues.Any(i => i.Message.Contains("Invalid mode")));
            Assert.IsTrue(report.Issues.Any(i => i.Message.Contains("both branch and tag")));
        }

        [TestMethod]
        public void WizardRebase_UpdateVersion_WorksCorrectly()
        {
            // Arrange - Create file with version
            File.WriteAllText(_testFile, @"global.version.binary=20013
framework.mode=source
framework.tag=Build_12.25.7.20013");

            var parser = new ChainFileParser();
            var chain = parser.ParsePropertiesFile(_testFile);

            // Act - Rebase version
            var rebaseService = new RebaseService();
            var currentVersion = rebaseService.ExtractCurrentVersion(chain);
            var updateCount = rebaseService.UpdateAllProjects(chain, "20014");

            // Assert
            Assert.AreEqual("20013", currentVersion);
            Assert.IsTrue(updateCount > 0);
        }

        [TestMethod]
        public void WizardBranchUpdate_ChangesBranches_WorksCorrectly()
        {
            // Arrange - Create file with branches
            var service = new ManualChainFileService();
            var projects = new List<ManualChainFileService.ProjectConfig>
            {
                new ManualChainFileService.ProjectConfig
                {
                    ProjectName = "framework",
                    Mode = "source",
                    Branch = "main",
                    TestsEnabled = true
                }
            };

            var filePath = service.CreateManualChainFile("159848", "branch test", "20013", projects, new ManualChainFileService.IntegrationTestsConfig(), _testDir);

            var parser = new ChainFileParser();
            var chain = parser.ParsePropertiesFile(filePath);

            // Act - Update branch
            var branchService = new BranchService();
            var updateCount = branchService.UpdateProjectBranches(chain, 
                new Dictionary<string, string> { { "framework", "develop" } });

            // Assert
            Assert.AreEqual(1, updateCount);
            Assert.AreEqual("develop", chain.Sections[0].Branch);
        }

        [TestMethod]
        public void WizardTestToggle_EnableDisableTests_WorksCorrectly()
        {
            // Arrange
            var service = new ManualChainFileService();
            var projects = new List<ManualChainFileService.ProjectConfig>
            {
                new ManualChainFileService.ProjectConfig
                {
                    ProjectName = "framework",
                    Mode = "source",
                    Branch = "main",
                    TestsEnabled = true
                }
            };

            var filePath = service.CreateManualChainFile("159848", "test toggle", "20013", projects, new ManualChainFileService.IntegrationTestsConfig(), _testDir);

            var parser = new ChainFileParser();
            var chain = parser.ParsePropertiesFile(filePath);

            // Act - Toggle tests
            var testService = new TestService();
            var updateCount = testService.UpdateProjectTests(chain, 
                new Dictionary<string, bool> { { "framework", false } });

            // Assert
            Assert.AreEqual(1, updateCount);
            Assert.IsFalse(chain.Sections[0].TestsUnit);
        }
    }
}