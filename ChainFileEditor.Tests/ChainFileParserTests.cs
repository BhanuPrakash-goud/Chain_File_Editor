using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class ChainFileParserTests
    {
        private ChainFileParser _parser = null!;
        private string _testFilePath = null!;

        [TestInitialize]
        public void Setup()
        {
            _parser = new ChainFileParser();
            _testFilePath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
        }

        [TestMethod]
        public void ParsePropertiesFile_ValidFile_ReturnsChainModel()
        {
            // Arrange
            var content = @"global.version.binary=1.0.0
framework.mode=source
framework.branch=main
app.mode=binary
app.tag=Build_1.0.0.1";
            File.WriteAllText(_testFilePath, content);

            // Act
            var result = _parser.ParsePropertiesFile(_testFilePath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("1.0.0", result.Global.VersionBinary);
            Assert.AreEqual(2, result.Sections.Count);
            Assert.AreEqual(content, result.RawContent);
        }

        [TestMethod]
        public void ParsePropertiesFile_WithComments_IgnoresComments()
        {
            // Arrange
            var content = @"# This is a comment
framework.mode=source
# Another comment
app.mode=binary";
            File.WriteAllText(_testFilePath, content);

            // Act
            var result = _parser.ParsePropertiesFile(_testFilePath);

            // Assert
            Assert.AreEqual(2, result.Sections.Count);
            Assert.AreEqual("source", result.Sections.First(s => s.Name == "framework").Mode);
        }

        [TestMethod]
        public void ParsePropertiesFile_WithTestsUnit_ParsesBooleanCorrectly()
        {
            // Arrange
            var content = @"framework.tests.unit=true
app.tests.unit=false";
            File.WriteAllText(_testFilePath, content);

            // Act
            var result = _parser.ParsePropertiesFile(_testFilePath);

            // Assert
            Assert.IsTrue(result.Sections.First(s => s.Name == "framework").TestsUnit);
            Assert.IsFalse(result.Sections.First(s => s.Name == "app").TestsUnit);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void ParsePropertiesFile_NonExistentFile_ThrowsException()
        {
            // Act
            _parser.ParsePropertiesFile("nonexistent.properties");
        }

        [TestMethod]
        public void ParsePropertiesFile_EmptyFile_ReturnsEmptyChain()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "");

            // Act
            var result = _parser.ParsePropertiesFile(_testFilePath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Sections.Count);
        }
    }
}