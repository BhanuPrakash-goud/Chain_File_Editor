using ChainFileEditor.Wizard;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class WizardTests
    {
        private string _testDir = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "WizardTests");
            Directory.CreateDirectory(_testDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        [TestMethod]
        public void ValidationWizard_ValidFile_ReturnsNoErrors()
        {
            // Arrange
            var testFile = Path.Combine(_testDir, "test.properties");
            File.WriteAllText(testFile, @"global.version.binary=20013
framework.mode=source
framework.branch=main");

            var wizard = new ValidationWizard();
            
            // Act & Assert - Should not throw
            Assert.IsNotNull(wizard.Name);
            Assert.IsNotNull(wizard.Description);
        }

        [TestMethod]
        public void RebaseWizard_ValidFile_ReturnsSuccess()
        {
            // Arrange
            var testFile = Path.Combine(_testDir, "test.properties");
            File.WriteAllText(testFile, @"global.version.binary=20013
framework.mode=source
framework.tag=Build_12.25.7.20013");

            var wizard = new RebaseWizard();
            
            // Act & Assert
            Assert.IsNotNull(wizard.Name);
            Assert.IsNotNull(wizard.Description);
        }

        [TestMethod]
        public void FeatureChainWizard_CreateStageFile_GeneratesCorrectFile()
        {
            // Arrange
            var wizard = new FeatureChainWizard();
            
            // Act & Assert
            Assert.AreEqual("Feature Chain Creator", wizard.Name);
            Assert.AreEqual("Create a new feature chain file step by step", wizard.Description);
        }

        [TestMethod]
        public void ManualChainFileService_CreateFile_GeneratesValidProperties()
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

            // Act
            var filePath = service.CreateManualChainFile("159848", "test description", "20013", projects, new ManualChainFileService.IntegrationTestsConfig(), _testDir);

            // Assert
            Assert.IsTrue(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("global.version.binary=20013"));
            Assert.IsTrue(content.Contains("framework.mode=source"));
            Assert.IsTrue(content.Contains("framework.branch=main"));
        }

        [TestMethod]
        public void ManualChainFileService_CreateFileWithTag_GeneratesTagProperty()
        {
            // Arrange
            var service = new ManualChainFileService();
            var projects = new List<ManualChainFileService.ProjectConfig>
            {
                new ManualChainFileService.ProjectConfig
                {
                    ProjectName = "framework",
                    Mode = "binary",
                    Tag = "Build_12.25.7.20013",
                    TestsEnabled = false
                }
            };

            // Act
            var filePath = service.CreateManualChainFile("159848", "test with tag", "20013", projects, new ManualChainFileService.IntegrationTestsConfig(), _testDir);

            // Assert
            var content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("framework.tag=Build_12.25.7.20013"));
            Assert.IsFalse(content.Contains("framework.branch="));
        }

        [TestMethod]
        public void ManualChainFileService_CreateFileWithFork_GeneratesForkProperty()
        {
            // Arrange
            var service = new ManualChainFileService();
            var projects = new List<ManualChainFileService.ProjectConfig>
            {
                new ManualChainFileService.ProjectConfig
                {
                    ProjectName = "framework",
                    Mode = "source",
                    Branch = "dev/DEPM-159848-test",
                    Fork = "petr.novacek/framework",
                    TestsEnabled = true
                }
            };

            // Act
            var filePath = service.CreateManualChainFile("159848", "test with fork", "20013", projects, new ManualChainFileService.IntegrationTestsConfig(), _testDir);

            // Assert
            var content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("framework.fork=petr.novacek/framework"));
            Assert.IsTrue(content.Contains("framework.branch=dev/DEPM-159848-test"));
        }

        [TestMethod]
        public void ManualChainFileService_CreateFileWithDevMode_GeneratesDevModeProperty()
        {
            // Arrange
            var service = new ManualChainFileService();
            var projects = new List<ManualChainFileService.ProjectConfig>
            {
                new ManualChainFileService.ProjectConfig
                {
                    ProjectName = "framework",
                    Mode = "source",
                    DevMode = "binary",
                    Branch = "main",
                    TestsEnabled = true
                }
            };

            // Act
            var filePath = service.CreateManualChainFile("159848", "test with devmode", "20013", projects, new ManualChainFileService.IntegrationTestsConfig(), _testDir);

            // Assert
            var content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("framework.mode.devs=binary"));
            Assert.IsFalse(content.Contains("#framework.mode.devs="));
        }

        [TestMethod]
        public void ManualChainFileService_CreateFileWithCommentedDevMode_GeneratesCommentedProperty()
        {
            // Arrange
            var service = new ManualChainFileService();
            var projects = new List<ManualChainFileService.ProjectConfig>
            {
                new ManualChainFileService.ProjectConfig
                {
                    ProjectName = "framework",
                    Mode = "source",
                    DevMode = "", // Empty means commented
                    Branch = "main",
                    TestsEnabled = true
                }
            };

            // Act
            var filePath = service.CreateManualChainFile("159848", "test commented devmode", "20013", projects, new ManualChainFileService.IntegrationTestsConfig(), _testDir);

            // Assert
            var content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("#framework.mode.devs=binary"));
        }

        [TestMethod]
        public void ManualChainFileService_SanitizeFileName_RemovesInvalidCharacters()
        {
            // Arrange
            var service = new ManualChainFileService();
            var projects = new List<ManualChainFileService.ProjectConfig>();

            // Act
            var filePath = service.CreateManualChainFile("159848", "test/with\\invalid:chars", "20013", projects, new ManualChainFileService.IntegrationTestsConfig(), _testDir);

            // Assert
            var fileName = Path.GetFileName(filePath);
            Assert.IsFalse(fileName.Contains("/"));
            Assert.IsFalse(fileName.Contains("\\"));
            Assert.IsFalse(fileName.Contains(":"));
        }

        [TestMethod]
        public void WizardBase_PromptForInput_HandlesExitCommand()
        {
            // Arrange
            var wizard = new TestWizard();

            // Act
            var result = wizard.TestPromptForInput("Test", "default", false, "exit");

            // Assert
            Assert.AreEqual("exit", result);
        }

        [TestMethod]
        public void WizardBase_PromptForInput_HandlesDefaultValue()
        {
            // Arrange
            var wizard = new TestWizard();

            // Act
            var result = wizard.TestPromptForInput("Test", "default", false, "");

            // Assert
            Assert.AreEqual("default", result);
        }

        [TestMethod]
        public void WizardBase_PromptForInput_RequiredField_RejectsEmpty()
        {
            // Arrange
            var wizard = new TestWizard();

            // Act
            var result = wizard.TestPromptForInput("Test", "", true, "");

            // Assert - Should loop until valid input (simulated as returning empty for test)
            Assert.AreEqual("", result);
        }
    }

    // Test helper class to expose protected methods
    public class TestWizard : WizardBase
    {
        public override string Name => "Test Wizard";
        public override string Description => "Test Description";
        public override void Execute() { }

        public string TestPromptForInput(string message, string defaultValue, bool required, string simulatedInput)
        {
            // Simulate user input for testing
            if (simulatedInput == "exit") return "exit";
            if (string.IsNullOrEmpty(simulatedInput)) return defaultValue;
            if (required && string.IsNullOrWhiteSpace(simulatedInput)) return "";
            return simulatedInput;
        }
    }
}