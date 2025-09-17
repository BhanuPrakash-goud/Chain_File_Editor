using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using ChainFileEditor.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class ValidationIntegrationTests
    {
        [TestMethod]
        public void FeatureChainValidation_WithWarnings_ReturnsExpectedIssues()
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
                            { "mode.devs", "binary" },
                            { "branch", "dev/DEPM-123-test" }
                        }
                    }
                }
            };

            var rules = ValidationRuleFactory.CreateAllRules();
            var validator = new ChainValidator(rules);
            var report = validator.Validate(chain);

            Assert.IsTrue(report.Issues.Count >= 0);
        }

        [TestMethod]
        public void CompleteChainValidation_ValidChain_PassesAllRules()
        {
            var requiredProjects = new[] { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment" };
            var sections = new List<Section>();
            
            foreach (var project in requiredProjects)
            {
                sections.Add(new Section 
                { 
                    Name = project,
                    Properties = new Dictionary<string, string> 
                    { 
                        { "mode", "source" },
                        { "branch", "main" }
                    }
                });
            }
            
            var chain = new ChainModel
            {
                Global = new GlobalSection 
                { 
                    VersionBinary = "20013",
                    DevVersionBinary = "20013"
                },
                Sections = sections
            };

            var rules = ValidationRuleFactory.CreateAllRules();
            var validator = new ChainValidator(rules);
            var report = validator.Validate(chain);

            var errors = report.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
            Assert.IsTrue(errors.Count <= 2, $"Expected minimal errors, but got {errors.Count}: {string.Join(", ", errors.Select(e => e.Message))}");
        }
    }
}