using ChainFileEditor.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class ChainModelTests
    {
        [TestMethod]
        public void Section_PropertyAccessors_WorkCorrectly()
        {
            // Arrange
            var section = new Section { Name = "test" };

            // Act & Assert
            section.Mode = "source";
            Assert.AreEqual("source", section.Mode);
            Assert.AreEqual("source", section.Properties["mode"]);

            section.Branch = "main";
            Assert.AreEqual("main", section.Branch);
            Assert.AreEqual("main", section.Properties["branch"]);

            section.Tag = "Build_1.0.0.1";
            Assert.AreEqual("Build_1.0.0.1", section.Tag);
            Assert.AreEqual("Build_1.0.0.1", section.Properties["tag"]);

            section.Fork = "origin/main";
            Assert.AreEqual("origin/main", section.Fork);
            Assert.AreEqual("origin/main", section.Properties["fork"]);

            section.DevMode = "binary";
            Assert.AreEqual("binary", section.DevMode);
            Assert.AreEqual("binary", section.Properties["mode.devs"]);
        }

        [TestMethod]
        public void Section_TestsUnit_HandlesBoolean()
        {
            // Arrange
            var section = new Section { Name = "test" };

            // Act & Assert
            section.TestsUnit = true;
            Assert.IsTrue(section.TestsUnit);
            Assert.AreEqual("true", section.Properties["tests.unit"]);

            section.TestsUnit = false;
            Assert.IsFalse(section.TestsUnit);
            Assert.AreEqual("false", section.Properties["tests.unit"]);
        }

        [TestMethod]
        public void Section_PropertyAccessors_HandleNullValues()
        {
            // Arrange
            var section = new Section { Name = "test" };

            // Act & Assert
            Assert.IsNull(section.Mode);
            Assert.IsNull(section.Branch);
            Assert.IsNull(section.Tag);
            Assert.IsNull(section.Fork);
            Assert.IsNull(section.DevMode);
            Assert.IsFalse(section.TestsUnit);
        }

        [TestMethod]
        public void Section_DirectPropertyModification_UpdatesProperties()
        {
            // Arrange
            var section = new Section 
            { 
                Name = "test",
                Properties = new Dictionary<string, string> { { "mode", "binary" } }
            };

            // Act
            section.Properties["mode"] = "source";

            // Assert
            Assert.AreEqual("source", section.Mode);
        }

        [TestMethod]
        public void GlobalSection_Properties_WorkCorrectly()
        {
            // Arrange
            var global = new GlobalSection();

            // Act & Assert
            global.VersionBinary = "1.0.0";
            Assert.AreEqual("1.0.0", global.VersionBinary);

            global.DevVersionBinary = "1.1.0";
            Assert.AreEqual("1.1.0", global.DevVersionBinary);
        }

        [TestMethod]
        public void ChainModel_Initialization_CreatesEmptyCollections()
        {
            // Act
            var chain = new ChainModel();

            // Assert
            Assert.IsNotNull(chain.Sections);
            Assert.IsNotNull(chain.Global);
            Assert.AreEqual(0, chain.Sections.Count);
        }

        [TestMethod]
        public void Section_BooleanParsing_HandlesStringValues()
        {
            // Arrange
            var section = new Section 
            { 
                Name = "test",
                Properties = new Dictionary<string, string> 
                { 
                    { "tests.unit", "true" },
                    { "tests", "false" }
                }
            };

            // Act & Assert
            Assert.IsTrue(section.TestsUnit);
            // TestsEnabled is a separate property, not from Properties dictionary
            section.TestsEnabled = false;
            Assert.IsFalse(section.TestsEnabled);
        }
    }
}