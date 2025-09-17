using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChainFileEditor.Core.Operations;
using System.IO;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class FeatureChainServiceTests
    {
        private FeatureChainService _service;
        private string _testOutputDir;

        [TestInitialize]
        public void Setup()
        {
            _service = new FeatureChainService();
            _testOutputDir = Path.Combine(Path.GetTempPath(), "ChainFileEditorTests");
            Directory.CreateDirectory(_testOutputDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, true);
            }
        }

        [TestMethod]
        public void CreateFeatureChainFile_ValidRequest_CreatesFile()
        {
            var request = new FeatureChainService.FeatureChainRequest
            {
                JiraId = "12345",
                Description = "test feature",
                Version = "20025",
                Projects = new[]
                {
                    new FeatureChainService.ProjectConfig
                    {
                        ProjectName = "framework",
                        Mode = "source",
                        Branch = "dev/feature",
                        TestsEnabled = true
                    }
                }.ToList()
            };

            var filePath = _service.CreateFeatureChainFile(request, _testOutputDir);

            Assert.IsTrue(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("global.version.binary=20025"));
            Assert.IsTrue(content.Contains("framework.mode=source"));
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void CreateFeatureChainFile_EmptyJiraId_ThrowsException()
        {
            var request = new FeatureChainService.FeatureChainRequest
            {
                JiraId = "",
                Description = "test"
            };

            _service.CreateFeatureChainFile(request, _testOutputDir);
        }

        [TestMethod]
        public void CreateFeatureChainFile_DevFeatureBranch_FormatsCorrectly()
        {
            var request = new FeatureChainService.FeatureChainRequest
            {
                JiraId = "12345",
                Description = "test feature",
                Projects = new[]
                {
                    new FeatureChainService.ProjectConfig
                    {
                        ProjectName = "framework",
                        Branch = "dev/feature",
                        ForkRepository = "user/framework"
                    }
                }.ToList()
            };

            var filePath = _service.CreateFeatureChainFile(request, _testOutputDir);
            var content = File.ReadAllText(filePath);

            Assert.IsTrue(content.Contains("dev/DEPM-12345-test-feature"));
            Assert.IsTrue(content.Contains("framework.fork=user/framework"));
        }
    }
}