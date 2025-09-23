using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChainFileEditor.Core.Models;

namespace ChainFileEditor.Core.Operations
{
    public sealed class ChainFileParser
    {
        private const string CommentPrefix = "#";
        private const string GlobalPrefix = "global.";
        private const string TestsPrefix = "tests.";
        private const string TestsRunSuffix = ".run";
        private const char PropertySeparator = '=';
        private const int PropertyParts = 2;
        public ChainModel ParsePropertiesFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Chain file not found: {filePath}");

            var content = File.ReadAllText(filePath);
            var lines = File.ReadAllLines(filePath);
            var properties = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith(CommentPrefix))
                    continue;

                var parts = line.Split(PropertySeparator, PropertyParts);
                if (parts.Length == PropertyParts)
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
            if (properties.TryGetValue(GlobalPropertyNames.VersionBinary, out var versionBinary))
                chain.Global.VersionBinary = versionBinary;
            
            if (properties.TryGetValue(GlobalPropertyNames.DevVersionBinary, out var devVersionBinary))
                chain.Global.DevVersionBinary = devVersionBinary;
            
            if (properties.TryGetValue(GlobalPropertyNames.Recipients, out var recipients))
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
            foreach (var kvp in properties.Where(p => p.Key.StartsWith(TestsPrefix) && p.Key.EndsWith(TestsRunSuffix)))
            {
                var testSuiteName = kvp.Key.Substring(TestsPrefix.Length, kvp.Key.Length - TestsPrefix.Length - TestsRunSuffix.Length);
                if (bool.TryParse(kvp.Value, out var isEnabled))
                {
                    chain.IntegrationTests.TestSuites[testSuiteName] = isEnabled;
                }
            }

            return chain;
        }

        private static HashSet<string> GetSectionNames(Dictionary<string, string> properties)
        {
            var sections = new HashSet<string>();
            
            foreach (var key in properties.Keys)
            {
                if (key.StartsWith(GlobalPrefix) || key.StartsWith(TestsPrefix)) continue;
                
                var parts = key.Split('.');
                if (parts.Length > 0)
                    sections.Add(parts[0]);
            }
            
            return sections;
        }
    }
    
    internal static class GlobalPropertyNames
    {
        public const string VersionBinary = "global.version.binary";
        public const string DevVersionBinary = "global.devs.version.binary";
        public const string Recipients = "global.recipients";
    }
}