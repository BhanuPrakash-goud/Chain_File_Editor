using System;
using System.Collections.Generic;
using System.Linq;
using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Constants;

namespace ChainFileEditor.Core.Operations
{
    public sealed class ModeService
    {
        public class ProjectModeInfo
        {
            public string ProjectName { get; set; } = string.Empty;
            public string CurrentMode { get; set; } = string.Empty;
            public string CurrentDevMode { get; set; } = string.Empty;
        }

        public class ProjectModeStatus
        {
            public string ProjectName { get; set; } = string.Empty;
            public string CurrentMode { get; set; } = string.Empty;
            public string CurrentDevMode { get; set; } = string.Empty;
        }

        public List<ProjectModeStatus> GetAllProjectModeStatus(ChainModel chain)
        {
            var projects = new List<ProjectModeStatus>();
            
            foreach (var section in chain.Sections)
            {
                projects.Add(new ProjectModeStatus
                {
                    ProjectName = section.Name,
                    CurrentMode = section.Mode ?? DefaultValues.SourceMode,
                    CurrentDevMode = section.DevMode ?? Messages.NotSet
                });
            }
            
            return projects;
        }

        public int UpdateProjectModes(ChainModel chain, Dictionary<string, (string mode, string devMode)> projectModes)
        {
            int updatedCount = 0;
            var validModes = GetValidModes();
            
            foreach (var kvp in projectModes)
            {
                var section = chain.Sections.FirstOrDefault(s => s.Name == kvp.Key);
                if (section != null)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value.mode) && validModes.Contains(kvp.Value.mode.ToLower()))
                    {
                        section.Mode = kvp.Value.mode;
                        updatedCount++;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(kvp.Value.devMode) && kvp.Value.devMode != Messages.NotSet)
                    {
                        if (validModes.Contains(kvp.Value.devMode.ToLower()))
                            section.DevMode = kvp.Value.devMode;
                        else if (kvp.Value.devMode == Messages.Clear)
                            section.DevMode = null;
                    }
                }
            }
            
            return updatedCount;
        }

        public List<string> GetValidModes()
        {
            return new List<string> { ModeValues.Source, ModeValues.Binary, ModeValues.Ignore };
        }

        public bool SetMode(ChainModel chain, string sectionName, string mode, string? devMode = null)
        {
            var validModes = GetValidModes();
            if (!validModes.Contains(mode.ToLower()))
                return false;

            var section = chain.Sections.FirstOrDefault(s => s.Name == sectionName);
            if (section == null)
                return false;

            section.Mode = mode;
            if (!string.IsNullOrWhiteSpace(devMode) && validModes.Contains(devMode.ToLower()))
                section.DevMode = devMode;

            return true;
        }

        public string[] GetProjectNames(ChainModel chain)
        {
            return chain.Sections.Select(s => s.Name).ToArray();
        }
    }
    

}