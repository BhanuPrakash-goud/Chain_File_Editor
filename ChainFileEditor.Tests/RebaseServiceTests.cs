using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class RebaseServiceTests
    {
        private RebaseService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new RebaseService();
        }

        [TestMethod]
        public void ExtractCurrentVersion_ReturnsGlobalVersion()
        {
            var chain = new ChainModel
            {
                Global = new GlobalSection { VersionBinary = "20025" }
            };

            var version = _service.ExtractCurrentVersion(chain);

            Assert.AreEqual("20025", version);
        }

        [TestMethod]
        public void AnalyzeProjectVersions_FindsVersionProperties()
        {
            var chain = new ChainModel
            {
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework",
                        Properties = new Dictionary<string, string> 
                        { 
                            { "tag", "Build_12.25.11.20025" },
                            { "version", "20025" }
                        }
                    },
                    new Section 
                    { 
                        Name = "repository",
                        Properties = new Dictionary<string, string> { { "mode", "source" } }
                    }
                }
            };

            var result = _service.AnalyzeProjectVersions(chain);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("framework", result[0].ProjectName);
            Assert.IsTrue(result[0].HasTag);
        }

        [TestMethod]
        public void UpdateSelectedProjects_UpdatesVersions()
        {
            var chain = new ChainModel
            {
                Global = new GlobalSection(),
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework",
                        Properties = new Dictionary<string, string> 
                        { 
                            { "tag", "Build_12.25.11.20025" }
                        }
                    }
                }
            };

            var selectedProjects = new List<string> { "framework" };
            var count = _service.UpdateSelectedProjects(chain, "20030", selectedProjects);

            Assert.AreEqual(1, count); // Only project tag updated
            Assert.AreEqual("Build_12.25.10.20030", chain.Sections[0].Properties["tag"]);
        }
    }
}