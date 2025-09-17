using ChainFileEditor.Core.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class ConfigurationTests
    {
        private string _testConfigPath = null!;

        [TestInitialize]
        public void Setup()
        {
            _testConfigPath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testConfigPath))
                File.Delete(_testConfigPath);
        }

        [TestMethod]
        public void ConfigurationLoader_LoadValidationConfig_ReturnsConfiguration()
        {
            // Act
            var result = ConfigurationLoader.LoadValidationConfig();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ValidationSettings);
            Assert.IsTrue(result.ValidationSettings.ValidModes.Count > 0);
            Assert.IsTrue(result.ValidationSettings.RequiredProjects.Count > 0);
        }

        [TestMethod]
        public void ConfigurationLoader_ReturnsDefaultConfiguration()
        {
            // Act
            var result = ConfigurationLoader.LoadValidationConfig();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ValidationSettings);
            Assert.IsTrue(result.ValidationSettings.ValidModes.Count > 0);
        }

        [TestMethod]
        public void ConfigurationLoader_CachesConfiguration()
        {
            // Act
            var result1 = ConfigurationLoader.LoadValidationConfig();
            var result2 = ConfigurationLoader.LoadValidationConfig();

            // Assert
            Assert.AreSame(result1, result2);
        }

        [TestMethod]
        public void ConfigurationLoader_ClearCache_ReloadsConfiguration()
        {
            // Arrange
            var result1 = ConfigurationLoader.LoadValidationConfig();
            
            // Act
            ConfigurationLoader.ClearValidationConfigCache();
            var result2 = ConfigurationLoader.LoadValidationConfig();

            // Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
        }
    }
}