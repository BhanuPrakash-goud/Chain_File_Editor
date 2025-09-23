using System;
using System.Collections.Generic;
using System.Linq;
using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Configuration;
using ChainFileEditor.Core.Constants;

namespace ChainFileEditor.Core.Operations
{
    public sealed class RebaseService
    {
        public int ReplaceVersion(ChainModel chain, string oldVersion, string newVersion)
        {
            var updated = 0;

            // Update global versions
            if (chain.Global.VersionBinary == oldVersion)
            {
                chain.Global.VersionBinary = newVersion;
                updated++;
            }
            
            if (chain.Global.DevVersionBinary == oldVersion)
            {
                chain.Global.DevVersionBinary = newVersion;
                updated++;
            }

            // Update section tags
            foreach (var section in chain.Sections)
            {
                if (section.Tag == oldVersion)
                {
                    section.Tag = newVersion;
                    updated++;
                }
                else if (!string.IsNullOrWhiteSpace(section.Tag) && section.Tag.Contains(oldVersion))
                {
                    section.Tag = section.Tag.Replace(oldVersion, newVersion);
                    updated++;
                }
            }

            return updated;
        }

        public int RebaseToNewVersion(ChainModel chain, string newVersion)
        {
            var currentVersions = GetCurrentVersions(chain);
            var updated = 0;

            foreach (var currentVersion in currentVersions)
            {
                updated += ReplaceVersion(chain, currentVersion, newVersion);
            }

            return updated;
        }

        public class ProjectVersionInfo
        {
            public string ProjectName { get; set; } = string.Empty;
            public string PropertyType { get; set; } = string.Empty;
            public string CurrentValue { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public bool HasTag { get; set; }
            public bool HasVersionBinary { get; set; }
        }

        public string ExtractCurrentVersion(ChainModel chain)
        {
            if (!string.IsNullOrWhiteSpace(chain.Global.VersionBinary))
                return chain.Global.VersionBinary;
                
            var depmServiceSection = chain.Sections.FirstOrDefault(s => s.Name.Equals(ProjectNames.DepmService, StringComparison.OrdinalIgnoreCase));
            if (depmServiceSection?.Tag != null && depmServiceSection.Tag.StartsWith(TagPrefixes.Build))
            {
                var parts = depmServiceSection.Tag.Split('.');
                if (parts.Length > 3)
                    return parts[3];
            }
            
            return Messages.NotFound;
        }

        public List<ProjectVersionInfo> AnalyzeProjectVersions(ChainModel chain)
        {
            var projects = new List<ProjectVersionInfo>();
            
            // Add global properties
            if (!string.IsNullOrWhiteSpace(chain.Global.VersionBinary))
            {
                projects.Add(new ProjectVersionInfo
                {
                    ProjectName = ProjectNames.Global,
                    PropertyType = PropertyTypes.VersionBinary,
                    CurrentValue = chain.Global.VersionBinary,
                    Status = Messages.GlobalVersionProperty,
                    HasVersionBinary = true
                });
            }
            
            if (!string.IsNullOrWhiteSpace(chain.Global.DevVersionBinary))
            {
                projects.Add(new ProjectVersionInfo
                {
                    ProjectName = ProjectNames.GlobalDevs,
                    PropertyType = PropertyTypes.VersionBinary,
                    CurrentValue = chain.Global.DevVersionBinary,
                    Status = Messages.GlobalDevVersionProperty,
                    HasVersionBinary = true
                });
            }
            
            // Only add project sections that have tags
            foreach (var section in chain.Sections.OrderBy(s => s.Name))
            {
                var hasTag = !string.IsNullOrWhiteSpace(section.Tag);
                
                if (hasTag)
                {
                    projects.Add(new ProjectVersionInfo
                    {
                        ProjectName = section.Name,
                        PropertyType = PropertyTypes.Tag,
                        CurrentValue = section.Tag,
                        Status = Messages.HasTag,
                        HasTag = true
                    });
                }
            }
            
            return projects;
        }

        public int UpdateSelectedProjects(ChainModel chain, string newVersion, List<string> selectedProjects)
        {
            var updated = 0;
            
            foreach (var projectName in selectedProjects)
            {
                if (projectName == ProjectNames.Global)
                {
                    chain.Global.VersionBinary = newVersion;
                    updated++;
                }
                else if (projectName == ProjectNames.GlobalDevs)
                {
                    chain.Global.DevVersionBinary = newVersion;
                    updated++;
                }
                else
                {
                    var section = chain.Sections.FirstOrDefault(s => s.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
                    if (section != null)
                    {
                        if (!string.IsNullOrWhiteSpace(section.Tag))
                        {
                            section.Tag = $"{TagFormats.BuildPrefix}{newVersion}";
                            updated++;
                        }
                    }
                }
            }
            
            return updated;
        }

        public static bool IsVersionInValidRange(string version)
        {
            if (int.TryParse(version, out var versionNumber))
            {
                var config = ConfigurationLoader.LoadValidationConfig();
                var minVersion = config.ValidationSettings.VersionRange.MinVersion;
                var maxVersion = config.ValidationSettings.VersionRange.MaxVersion;
                return versionNumber >= minVersion && versionNumber <= maxVersion;
            }
            return false;
        }

        public static string GetVersionRangeWarningMessage(string version)
        {
            var config = ConfigurationLoader.LoadValidationConfig();
            var minVersion = config.ValidationSettings.VersionRange.MinVersion;
            var maxVersion = config.ValidationSettings.VersionRange.MaxVersion;
            return $"Warning: Version {version} is outside the recommended range ({minVersion}-{maxVersion}). Do you want to continue?";
        }

        public int UpdateAllProjects(ChainModel chain, string newVersion)
        {
            int updatesCount = 0;
            
            // Update global version
            if (!string.IsNullOrWhiteSpace(chain.Global.VersionBinary))
            {
                chain.Global.VersionBinary = newVersion;
                updatesCount++;
            }
            
            if (!string.IsNullOrWhiteSpace(chain.Global.DevVersionBinary))
            {
                chain.Global.DevVersionBinary = newVersion;
                updatesCount++;
            }
            
            // Update all project tags
            foreach (var section in chain.Sections)
            {
                if (!string.IsNullOrWhiteSpace(section.Tag))
                {
                    section.Tag = $"{TagFormats.BuildPrefix}{newVersion}";
                    updatesCount++;
                }
            }
            
            return updatesCount;
        }

        public string[] GetCurrentVersions(ChainModel chain)
        {
            var versions = new HashSet<string>();

            if (!string.IsNullOrWhiteSpace(chain.Global.VersionBinary))
                versions.Add(chain.Global.VersionBinary);
            
            if (!string.IsNullOrWhiteSpace(chain.Global.DevVersionBinary))
                versions.Add(chain.Global.DevVersionBinary);

            foreach (var section in chain.Sections)
            {
                if (!string.IsNullOrWhiteSpace(section.Tag))
                    versions.Add(section.Tag);
            }

            return versions.ToArray();
        }
    }
    

}