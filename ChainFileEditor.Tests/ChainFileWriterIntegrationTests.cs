using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class ChainFileWriterIntegrationTests
    {
        private ChainFileWriter _writer;
        private ChainFileParser _parser;
        private string _testFilePath;

        [TestInitialize]
        public void Setup()
        {
            _writer = new ChainFileWriter();
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
        public void WritePropertiesFile_GlobalDevVersionManagement_WorksCorrectly()
        {
            var chain = new ChainModel
            {
                Global = new GlobalSection { VersionBinary = "20013" },
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework",
                        Properties = new Dictionary<string, string> 
                        { 
                            { "mode", "source" },
                            { "mode.devs", "binary" }
                        }
                    }
                },
                RawContent = "global.version.binary=20013\n#global.devs.version.binary=10XXX\n\nframework.mode=source\nframework.mode.devs=binary"
            };

            _writer.WritePropertiesFile(_testFilePath, chain);
            var content = File.ReadAllText(_testFilePath);

            Assert.IsTrue(content.Contains("global.devs.version.binary=20013"));
            Assert.IsFalse(content.Contains("#global.devs.version.binary"));
        }

        [TestMethod]
        public void WritePropertiesFile_NoDevsBinary_CommentsGlobalDevVersion()
        {
            var chain = new ChainModel
            {
                Global = new GlobalSection { VersionBinary = "20013" },
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework",
                        Properties = new Dictionary<string, string> { { "mode", "source" } }
                    }
                },
                RawContent = "global.version.binary=20013\nglobal.devs.version.binary=20013\n\nframework.mode=source"
            };

            _writer.WritePropertiesFile(_testFilePath, chain);
            var content = File.ReadAllText(_testFilePath);

            Assert.IsTrue(content.Contains("#global.devs.version.binary"));
        }

        [TestMethod]
        public void WritePropertiesFile_PreservesOriginalStructure()
        {
            var originalContent = "# Comment line\nglobal.version.binary=20013\n\nframework.mode=source\nframework.branch=main\n\n# Another comment\nrepository.mode=binary";
            
            var chain = _parser.ParsePropertiesFile(_testFilePath);
            chain.RawContent = originalContent;
            
            _writer.WritePropertiesFile(_testFilePath, chain);
            var newContent = File.ReadAllText(_testFilePath);
            
            var originalLines = originalContent.Split('\n');
            var newLines = newContent.Split('\n');
            
            Assert.IsTrue(Math.Abs(originalLines.Length - newLines.Length) <= 1, $"Expected similar line count, original: {originalLines.Length}, new: {newLines.Length}");
        }
    }
}