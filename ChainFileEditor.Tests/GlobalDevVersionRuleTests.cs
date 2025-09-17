using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChainFileEditor.Core.Validation.Rules;
using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Validation;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class GlobalDevVersionRuleTests
    {
        private GlobalDevVersionRequiredRule _rule;

        [TestInitialize]
        public void Setup()
        {
            _rule = new GlobalDevVersionRequiredRule();
        }

        [TestMethod]
        public void GlobalDevVersionRequired_WithDevsBinary_RequiresGlobalDevVersion()
        {
            var chain = new ChainModel
            {
                Global = new GlobalSection(),
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework",
                        Properties = new Dictionary<string, string> { { "mode.devs", "binary" } }
                    }
                }
            };

            var result = _rule.Validate(chain);

            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(ValidationSeverity.Warning, result.Issues[0].Severity);
            Assert.IsTrue(result.Issues[0].Message.Contains("global.devs.version.binary is required"));
        }

        [TestMethod]
        public void GlobalDevVersionRequired_WithDevsBinaryAndGlobalVersion_Passes()
        {
            var chain = new ChainModel
            {
                Global = new GlobalSection { DevVersionBinary = "20013" },
                Sections = new List<Section>
                {
                    new Section 
                    { 
                        Name = "framework",
                        Properties = new Dictionary<string, string> { { "mode.devs", "binary" } }
                    }
                }
            };

            var result = _rule.Validate(chain);

            Assert.AreEqual(0, result.Issues.Count);
        }

        [TestMethod]
        public void GlobalDevVersionRequired_NoDevsBinary_Passes()
        {
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

            var result = _rule.Validate(chain);

            Assert.AreEqual(0, result.Issues.Count);
        }
    }
}