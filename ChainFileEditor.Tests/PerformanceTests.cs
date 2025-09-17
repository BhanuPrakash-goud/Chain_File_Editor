using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChainFileEditor.Tests
{
    [TestClass]
    public class PerformanceTests
    {
        [TestMethod]
        public void ValidationPerformance_LargeChain_CompletesQuickly()
        {
            var chain = CreateLargeChain(100);
            var rules = ValidationRuleFactory.CreateAllRules();
            var validator = new ChainValidator(rules);

            var stopwatch = Stopwatch.StartNew();
            var report = validator.Validate(chain);
            stopwatch.Stop();

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Validation took {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsNotNull(report);
        }

        [TestMethod]
        public void RebasePerformance_ManyProjects_CompletesQuickly()
        {
            var chain = CreateLargeChain(50);
            var service = new RebaseService();
            var selectedProjects = chain.Sections.Select(s => s.Name).ToList();

            var stopwatch = Stopwatch.StartNew();
            var count = service.UpdateSelectedProjects(chain, "20014", selectedProjects);
            stopwatch.Stop();

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, $"Rebase took {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(count >= 0);
        }

        [TestMethod]
        public void ParsingPerformance_LargeFile_CompletesQuickly()
        {
            var content = GenerateLargeChainContent(200);
            var parser = new ChainFileParser();
            var tempFile = Path.GetTempFileName();
            
            try
            {
                File.WriteAllText(tempFile, content);
                
                var stopwatch = Stopwatch.StartNew();
                var chain = parser.ParsePropertiesFile(tempFile);
                stopwatch.Stop();

                Assert.IsTrue(stopwatch.ElapsedMilliseconds < 200, $"Parsing took {stopwatch.ElapsedMilliseconds}ms");
                Assert.IsTrue(chain.Sections.Count > 0);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        private ChainModel CreateLargeChain(int projectCount)
        {
            var sections = new List<Section>();
            for (int i = 0; i < projectCount; i++)
            {
                sections.Add(new Section
                {
                    Name = $"project{i}",
                    Properties = new Dictionary<string, string>
                    {
                        { "mode", "source" },
                        { "branch", "main" },
                        { "tests.unit", "true" }
                    }
                });
            }

            return new ChainModel
            {
                Global = new GlobalSection { VersionBinary = "20013" },
                Sections = sections
            };
        }

        private string GenerateLargeChainContent(int projectCount)
        {
            var lines = new List<string> { "global.version.binary=20013", "" };
            
            for (int i = 0; i < projectCount; i++)
            {
                lines.Add($"project{i}.mode=source");
                lines.Add($"project{i}.branch=main");
                lines.Add($"project{i}.tests.unit=true");
                lines.Add("");
            }

            return string.Join("\n", lines);
        }
    }
}