using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class ModeServiceTests
    {
        private ModeService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new ModeService();
        }

        [TestMethod]
        public void GetValidModes_ReturnsExpectedModes()
        {
            var modes = _service.GetValidModes();

            Assert.IsTrue(modes.Contains("source"));
            Assert.IsTrue(modes.Contains("binary"));
            Assert.IsTrue(modes.Contains("ignore"));
        }

        [TestMethod]
        public void GetAllProjectModeStatus_ReturnsCorrectStatus()
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
                            { "mode", "source" },
                            { "mode.devs", "binary" }
                        }
                    }
                }
            };

            var result = _service.GetAllProjectModeStatus(chain);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("source", result[0].CurrentMode);
            Assert.AreEqual("binary", result[0].CurrentDevMode);
        }

        [TestMethod]
        public void UpdateProjectModes_UpdatesCorrectly()
        {
            var chain = new ChainModel
            {
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework",
                        Properties = new Dictionary<string, string> { { "mode", "source" } }
                    }
                }
            };

            var updates = new Dictionary<string, (string mode, string devMode)> 
            { 
                { "framework", ("binary", "ignore") } 
            };
            var count = _service.UpdateProjectModes(chain, updates);

            Assert.AreEqual(1, count);
            Assert.AreEqual("binary", chain.Sections[0].Properties["mode"]);
            Assert.AreEqual("ignore", chain.Sections[0].Properties["mode.devs"]);
        }
    }
}