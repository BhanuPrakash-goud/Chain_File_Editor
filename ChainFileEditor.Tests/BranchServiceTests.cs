using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class BranchServiceTests
    {
        private BranchService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new BranchService();
        }

        [TestMethod]
        public void GetAllProjectBranchStatus_ReturnsCorrectStatus()
        {
            var chain = new ChainModel
            {
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework",
                        Properties = new Dictionary<string, string> { { "branch", "main" } }
                    },
                    new Section 
                    { 
                        Name = "repository",
                        Properties = new Dictionary<string, string> { { "tag", "Build_12.25.11.20025" } }
                    }
                }
            };

            var result = _service.GetAllProjectBranchStatus(chain);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("main", result.First(r => r.ProjectName == "framework").CurrentBranch);
            Assert.AreEqual("(no branch)", result.First(r => r.ProjectName == "repository").CurrentBranch);
        }

        [TestMethod]
        public void UpdateProjectBranches_UpdatesCorrectly()
        {
            var chain = new ChainModel
            {
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework",
                        Properties = new Dictionary<string, string> { { "branch", "main" } }
                    }
                }
            };

            var updates = new Dictionary<string, string> { { "framework", "stage" } };
            var count = _service.UpdateProjectBranches(chain, updates);

            Assert.AreEqual(1, count);
            Assert.AreEqual("stage", chain.Sections[0].Properties["branch"]);
        }

        [TestMethod]
        public void UpdateProjectBranches_RemovesTagWhenBranchSet()
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
                            { "branch", "main" }
                        }
                    }
                }
            };

            var updates = new Dictionary<string, string> { { "framework", "stage" } };
            _service.UpdateProjectBranches(chain, updates);

            Assert.IsFalse(chain.Sections[0].Properties.ContainsKey("tag"));
            Assert.AreEqual("stage", chain.Sections[0].Properties["branch"]);
        }
    }
}