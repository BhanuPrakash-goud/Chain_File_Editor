using System.IO;
using System.Linq;
using System.Text;
using ChainFileEditor.Core.Models;
using System;
using System.Collections.Generic;

namespace ChainFileEditor.Core.Operations
{
    public sealed class ChainFileWriter
    {
        private static readonly string[] ProjectOrder = { "framework", "repository", "olap", "modeling", "depmservice", "consolidation", "appengine", "designer", "dashboards", "appstudio", "officeinteg", "administration", "content", "deployment", "tests" };
        private static readonly string[] PropertyOrder = { "mode", "mode.devs", "fork", "branch", "tag", "tests.unit" };
        public void WritePropertiesFile(string filePath, ChainModel chain)
        {
            if (!string.IsNullOrEmpty(chain.RawContent))
            {
                // Preserve existing structure and add missing content in correct order
                WriteWithOrderedInsertions(filePath, chain);
            }
            else
            {
                // Fallback to structured writing
                WriteStructuredFile(filePath, chain);
            }
        }
        
        private void UpdatePropertyInLines(List<string> lines, string propertyKey, string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyValue)) return;
            
            // First try to find and update existing property
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith($"{propertyKey}=") || line.StartsWith($"#{propertyKey}="))
                {
                    lines[i] = $"{propertyKey}={propertyValue}";
                    return;
                }
            }
            
            // If property doesn't exist, add it in correct order within the section
            var sectionName = propertyKey.Split('.')[0];
            var propertyName = propertyKey.Substring(sectionName.Length + 1);
            var targetPropertyIndex = Array.IndexOf(PropertyOrder, propertyName);
            
            if (targetPropertyIndex >= 0)
            {
                // Find where to insert based on property order
                int insertIndex = FindPropertyInsertionPoint(lines, sectionName, propertyName, PropertyOrder);
                if (insertIndex >= 0)
                {
                    lines.Insert(insertIndex, $"{propertyKey}={propertyValue}");
                }
            }
        }
        
        private int FindPropertyInsertionPoint(List<string> lines, string sectionName, string propertyName, string[] propertyOrder)
        {
            var targetIndex = Array.IndexOf(propertyOrder, propertyName);
            if (targetIndex < 0) return -1;
            
            // Find the section boundaries
            int sectionStart = -1, sectionEnd = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith($"{sectionName}.") && !line.StartsWith("#"))
                {
                    if (sectionStart < 0) sectionStart = i;
                    sectionEnd = i;
                }
            }
            
            if (sectionStart < 0) return -1;
            
            // Find insertion point based on property order
            for (int propIndex = targetIndex + 1; propIndex < propertyOrder.Length; propIndex++)
            {
                var nextProp = propertyOrder[propIndex];
                for (int i = sectionStart; i <= sectionEnd; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith($"{sectionName}.{nextProp}="))
                    {
                        return i; // Insert before this property
                    }
                }
            }
            
            // Insert after the last property of this section
            return sectionEnd + 1;
        }
        

        
        private void WriteWithOrderedInsertions(string filePath, ChainModel chain)
        {
            var lines = chain.RawContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            
            // Update existing properties
            foreach (var section in chain.Sections)
            {
                foreach (var property in section.Properties)
                {
                    var propertyKey = $"{section.Name}.{property.Key}";
                    UpdatePropertyInLines(lines, propertyKey, property.Value);
                }
            }
            
            // Update global properties
            if (chain.Global != null)
            {
                UpdatePropertyInLines(lines, "global.version.binary", chain.Global.VersionBinary);
                UpdatePropertyInLines(lines, "global.devs.version.binary", chain.Global.DevVersionBinary);
                UpdatePropertyInLines(lines, "global.recipients", chain.Global.Recipients);
            }
            
            // Add missing sections in correct order
            var existingSections = GetExistingSectionNames(lines);
            foreach (var projectName in ProjectOrder)
            {
                if (!existingSections.Contains(projectName, StringComparer.OrdinalIgnoreCase))
                {
                    var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                    if (section != null)
                    {
                        InsertSectionInOrder(lines, section, ProjectOrder);
                    }
                }
            }
            
            File.WriteAllLines(filePath, lines);
        }
        
        private HashSet<string> GetExistingSectionNames(List<string> lines)
        {
            var sections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("#") && trimmed.Contains("=") && !trimmed.StartsWith("global."))
                {
                    var parts = trimmed.Split('.');
                    if (parts.Length >= 2)
                    {
                        sections.Add(parts[0]);
                    }
                }
            }
            return sections;
        }
        
        private void InsertSectionInOrder(List<string> lines, Section section, string[] projectOrder)
        {
            var targetIndex = Array.IndexOf(projectOrder, section.Name.ToLower());
            if (targetIndex < 0) return;
            
            // Find insertion point - after the last project that should come before this one
            int insertIndex = lines.Count;
            for (int i = targetIndex - 1; i >= 0; i--)
            {
                var prevProject = projectOrder[i];
                var lastLineIndex = FindLastLineForSection(lines, prevProject);
                if (lastLineIndex >= 0)
                {
                    insertIndex = lastLineIndex + 1;
                    break;
                }
            }
            
            // If no previous project found, insert after global properties
            if (insertIndex == lines.Count)
            {
                var globalEndIndex = FindGlobalPropertiesEnd(lines);
                if (globalEndIndex >= 0)
                {
                    insertIndex = globalEndIndex + 1;
                }
            }
            
            // Add empty line before section if needed
            if (insertIndex > 0 && !string.IsNullOrWhiteSpace(lines[insertIndex - 1]))
            {
                lines.Insert(insertIndex, "");
                insertIndex++;
            }
            
            // Insert section properties
            foreach (var propKey in PropertyOrder)
            {
                if (section.Properties.ContainsKey(propKey))
                {
                    var value = section.Properties[propKey];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        lines.Insert(insertIndex++, $"{section.Name}.{propKey}={value}");
                    }
                }
            }
            
            // Add empty line after section
            lines.Insert(insertIndex, "");
        }
        
        private int FindLastLineForSection(List<string> lines, string sectionName)
        {
            int lastIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith($"{sectionName}.", StringComparison.OrdinalIgnoreCase) && !line.StartsWith("#"))
                {
                    lastIndex = i;
                }
            }
            return lastIndex;
        }
        
        private int FindGlobalPropertiesEnd(List<string> lines)
        {
            int lastGlobalIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("global.") && !line.StartsWith("#"))
                {
                    lastGlobalIndex = i;
                }
            }
            return lastGlobalIndex;
        }
        
        private void WriteStructuredFile(string filePath, ChainModel chain)
        {
            var sb = new StringBuilder();

            // Write header comments
            sb.AppendLine("# Feature chain configuration file");
            sb.AppendLine("#");
            sb.AppendLine("# JIRA: DEPM-123456");
            sb.AppendLine("# Description: Sample");
            sb.AppendLine();

            // Write global settings
            if (chain.Global != null)
            {
                if (!string.IsNullOrWhiteSpace(chain.Global.VersionBinary))
                    sb.AppendLine($"global.version.binary={chain.Global.VersionBinary}");
                
                if (!string.IsNullOrWhiteSpace(chain.Global.DevVersionBinary))
                    sb.AppendLine($"global.devs.version.binary={chain.Global.DevVersionBinary}");
                
                if (!string.IsNullOrWhiteSpace(chain.Global.Recipients))
                    sb.AppendLine($"global.recipients={chain.Global.Recipients}");
            }

            sb.AppendLine();

            // Write projects in correct order
            foreach (var projectName in ProjectOrder)
            {
                var section = chain.Sections?.FirstOrDefault(s => s.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                if (section != null)
                {
                    WriteSectionProperties(sb, section);
                }
            }
            
            // Write any remaining sections not in the standard order
            var extraSections = chain.Sections?.Where(s => !ProjectOrder.Contains(s.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            if (extraSections?.Count > 0)
            {
                foreach (var section in extraSections)
                {
                    WriteSectionProperties(sb, section);
                }
            }

            File.WriteAllText(filePath, sb.ToString());
        }
        
        private void WriteSectionProperties(StringBuilder sb, Section section)
        {
            // Write properties in specific order
            foreach (var propKey in PropertyOrder)
            {
                if (section.Properties.ContainsKey(propKey))
                {
                    var value = section.Properties[propKey];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        sb.AppendLine($"{section.Name}.{propKey}={value}");
                    }
                }
                else if (propKey == "mode.devs" && !section.Properties.ContainsKey(propKey))
                {
                    // Add commented mode.devs if not present
                    sb.AppendLine($"#{section.Name}.mode.devs=binary");
                }
            }
            
            // Write any other properties not in the standard order
            foreach (var prop in section.Properties)
            {
                if (!PropertyOrder.Contains(prop.Key) && !string.IsNullOrWhiteSpace(prop.Value))
                {
                    sb.AppendLine($"{section.Name}.{prop.Key}={prop.Value}");
                }
            }

            sb.AppendLine();
        }
    }
}