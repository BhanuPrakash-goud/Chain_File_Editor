using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Models;
using System.IO;
using System.Collections.Generic;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class ChainFileWriterTests
    {
        private ChainFileWriter _writer;
        private string _testFilePath;

        [TestInitialize]
        public void Setup()
        {
            _writer = new ChainFileWriter();
            _testFilePath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [TestMethod]
        public void WritePropertiesFile_WithRawContent_PreservesOrder()
        {
            var chain = new ChainModel
            {
                RawContent = "framework.mode=source\nframework.branch=main\nrepository.mode=binary",
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework", 
                        Mode = "binary",
                        Branch = "stage"
                    }
                }
            };

            _writer.WritePropertiesFile(_testFilePath, chain);

            var content = File.ReadAllText(_testFilePath);
            Assert.IsTrue(content.Contains("framework.mode=binary"));
            Assert.IsTrue(content.Contains("framework.branch=stage"));
        }

        [TestMethod]
        public void WritePropertiesFile_WithoutRawContent_UsesStructuredFormat()
        {
            var chain = new ChainModel
            {
                Global = new GlobalSection { VersionBinary = "20025" },
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework", 
                        Mode = "source",
                        Branch = "main"
                    }
                }
            };

            _writer.WritePropertiesFile(_testFilePath, chain);

            var content = File.ReadAllText(_testFilePath);
            Assert.IsTrue(content.Contains("global.version.binary=20025"));
            Assert.IsTrue(content.Contains("framework.mode=source"));
            Assert.IsTrue(content.Contains("framework.branch=main"));
        }
    }
}