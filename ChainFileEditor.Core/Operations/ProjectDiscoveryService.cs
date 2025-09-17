using System;
using System.Collections.Generic;
using System.Linq;
using ChainFileEditor.Core.Models;

namespace ChainFileEditor.Core.Operations
{
    public sealed class ProjectDiscoveryService
    {
        public string[] GetAllProjects(ChainModel chain)
        {
            return chain.Sections.Select(s => s.Name).ToArray();
        }

        public string[] GetProjectsByType(ChainModel chain, ProjectType type)
        {
            return type switch
            {
                ProjectType.Test => GetTestProjects(chain),
                ProjectType.Binary => GetBinaryProjects(chain),
                ProjectType.Source => GetSourceProjects(chain),
                ProjectType.Framework => GetFrameworkProjects(chain),
                _ => GetAllProjects(chain)
            };
        }

        public string[] GetTestProjects(ChainModel chain)
        {
            return chain.Sections.Where(s => IsTestProject(s))
                                 .Select(s => s.Name)
                                 .ToArray();
        }

        public string[] GetBinaryProjects(ChainModel chain)
        {
            return chain.Sections.Where(s => s.Mode?.ToLower() == "binary")
                                 .Select(s => s.Name)
                                 .ToArray();
        }

        public string[] GetSourceProjects(ChainModel chain)
        {
            return chain.Sections.Where(s => s.Mode?.ToLower() == "source")
                                 .Select(s => s.Name)
                                 .ToArray();
        }

        public string[] GetFrameworkProjects(ChainModel chain)
        {
            return chain.Sections.Where(s => IsFrameworkProject(s))
                                 .Select(s => s.Name)
                                 .ToArray();
        }

        public ProjectInfo[] GetProjectDetails(ChainModel chain)
        {
            return chain.Sections.Select(s => new ProjectInfo
            {
                Name = s.Name,
                Mode = s.Mode ?? "unknown",
                Branch = s.Branch,
                Tag = s.Tag,
                HasTests = s.TestsUnit,
                Type = DetermineProjectType(s)
            }).ToArray();
        }

        private bool IsTestProject(Section section)
        {
            return section.Name.ToLower().Contains("test") || 
                   section.TestsUnit == true;
        }

        private bool IsFrameworkProject(Section section)
        {
            var frameworkKeywords = new[] { "framework", "core", "base", "common" };
            return frameworkKeywords.Any(keyword => 
                section.Name.ToLower().Contains(keyword));
        }

        private ProjectType DetermineProjectType(Section section)
        {
            if (IsTestProject(section)) return ProjectType.Test;
            if (IsFrameworkProject(section)) return ProjectType.Framework;
            if (section.Mode?.ToLower() == "binary") return ProjectType.Binary;
            if (section.Mode?.ToLower() == "source") return ProjectType.Source;
            return ProjectType.Unknown;
        }

        public string[] GetCommonProjectNames()
        {
            return new[] 
            { 
                "framework", "repository", "content", "tests", 
                "api", "ui", "database", "services", "core", 
                "common", "shared", "utils", "tools" 
            };
        }
    }

    public enum ProjectType
    {
        Unknown,
        Test,
        Binary,
        Source,
        Framework
    }

    public class ProjectInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public string? Tag { get; set; }
        public bool HasTests { get; set; }
        public ProjectType Type { get; set; }
    }
}