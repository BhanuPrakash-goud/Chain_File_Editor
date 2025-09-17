using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Models;
using System.Collections.Generic;
using System.IO;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class EdgeCaseTests
    {
        [TestMethod]
        public void ChainFileParser_EmptyProperties_HandlesGracefully()
        {
            var parser = new ChainFileParser();
            var tempFile = Path.GetTempFileName();
            
            try
            {
                File.WriteAllText(tempFile, "framework.mode=\nrepository.branch=");
                var chain = parser.ParsePropertiesFile(tempFile);
                
                Assert.AreEqual(2, chain.Sections.Count);
                Assert.AreEqual("", chain.Sections[0].Properties["mode"]);
                Assert.AreEqual("", chain.Sections[1].Properties["branch"]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void RebaseService_VersionRangeValidation_WorksCorrectly()
        {
            Assert.IsTrue(RebaseService.IsVersionInValidRange("15000"));
            Assert.IsFalse(RebaseService.IsVersionInValidRange("5000"));
            Assert.IsFalse(RebaseService.IsVersionInValidRange("50000"));
            Assert.IsFalse(RebaseService.IsVersionInValidRange("invalid"));
        }

        [TestMethod]
        public void FeatureChainService_SpecialCharacters_HandlesCorrectly()
        {
            var service = new FeatureChainService();
            var request = new FeatureChainService.FeatureChainRequest
            {
                JiraId = "DEPM-123",
                Description = "test with spaces & symbols",
                Version = "20013",
                Projects = new List<FeatureChainService.ProjectConfig>
                {
                    new FeatureChainService.ProjectConfig
                    {
                        ProjectName = "framework",
                        Mode = "source",
                        Branch = "main"
                    }
                }
            };

            var tempDir = Path.GetTempPath();
            var result = service.CreateFeatureChainFile(request, tempDir);
            
            Assert.IsTrue(File.Exists(result));
            var content = File.ReadAllText(result);
            Assert.IsTrue(content.Contains("framework.mode=source"));
            
            File.Delete(result);
        }

        [TestMethod]
        public void ChainFileWriter_NullValues_HandlesGracefully()
        {
            var writer = new ChainFileWriter();
            var chain = new ChainModel
            {
                Global = new GlobalSection(),
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework",
                        Properties = new Dictionary<string, string> { { "mode", "source" } }
                    }
                }
            };

            var tempFile = Path.GetTempFileName();
            try
            {
                writer.WritePropertiesFile(tempFile, chain);
                Assert.IsTrue(File.Exists(tempFile));
                var content = File.ReadAllText(tempFile);
                Assert.IsTrue(content.Contains("framework.mode=source"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void BranchService_DuplicateProjects_HandlesCorrectly()
        {
            var service = new BranchService();
            var chain = new ChainModel
            {
                Sections = new List<Section>
                {
                    new Section { Name = "framework", Properties = new Dictionary<string, string> { { "branch", "main" } } },
                    new Section { Name = "framework", Properties = new Dictionary<string, string> { { "branch", "stage" } } }
                }
            };

            var updates = new Dictionary<string, string> { { "framework", "integration" } };
            var count = service.UpdateProjectBranches(chain, updates);
            
            Assert.AreEqual(1, count); // Only first matching project updated
        }
    }
}