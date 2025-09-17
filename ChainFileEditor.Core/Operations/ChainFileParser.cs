using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChainFileEditor.Core.Models;

namespace ChainFileEditor.Core.Operations
{
    public sealed class ChainFileParser
    {
        public ChainModel ParsePropertiesFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Chain file not found: {filePath}");

            var content = File.ReadAllText(filePath);
            var lines = File.ReadAllLines(filePath);
            var properties = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    properties[parts[0].Trim()] = parts[1].Trim();
                }
            }

            var chain = ConvertToChainModel(properties);
            chain.RawContent = content;
            return chain;
        }

        private ChainModel ConvertToChainModel(Dictionary<string, string> properties)
        {
            var chain = new ChainModel();

            // Parse global settings
            if (properties.TryGetValue("global.version.binary", out var versionBinary))
                chain.Global.VersionBinary = versionBinary;
            
            if (properties.TryGetValue("global.devs.version.binary", out var devVersionBinary))
                chain.Global.DevVersionBinary = devVersionBinary;
            
            if (properties.TryGetValue("global.recipients", out var recipients))
                chain.Global.Recipients = recipients;

            // Parse sections
            var sectionNames = GetSectionNames(properties);
            foreach (var sectionName in sectionNames)
            {
                var section = new Section { Name = sectionName };

                // Populate Properties dictionary with all section-specific properties
                foreach (var kvp in properties.Where(p => p.Key.StartsWith($"{sectionName}.")))
                {
                    var propertyName = kvp.Key.Substring(sectionName.Length + 1);
                    section.Properties[propertyName] = kvp.Value;
                }

                chain.Sections.Add(section);
            }

            // Parse integration tests
            foreach (var kvp in properties.Where(p => p.Key.StartsWith("tests.") && p.Key.EndsWith(".run")))
            {
                var testSuiteName = kvp.Key.Substring(6, kvp.Key.Length - 10); // Remove "tests." and ".run"
                if (bool.TryParse(kvp.Value, out var isEnabled))
                {
                    chain.IntegrationTests.TestSuites[testSuiteName] = isEnabled;
                }
            }

            return chain;
        }

        private HashSet<string> GetSectionNames(Dictionary<string, string> properties)
        {
            var sections = new HashSet<string>();
            
            foreach (var key in properties.Keys)
            {
                if (key.StartsWith("global.") || key.StartsWith("tests.")) continue;
                
                var parts = key.Split('.');
                if (parts.Length > 0)
                    sections.Add(parts[0]);
            }
            
            return sections;
        }
    }
}