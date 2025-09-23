using ChainFileEditor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using ChainFileEditor.Core.Constants;

namespace ChainFileEditor.Core.Operations
{
    public class BranchService
    {
        public class BranchInfo
        {
            public string BranchName { get; set; } = string.Empty;
            public string BranchType { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public List<string> GetProjectsFromFile(ChainModel chain)
        {
            return chain.Sections.Select(s => s.Name).ToList();
        }

        public List<BranchInfo> GetBranchTypesForProject(string projectName)
        {
            var branches = new List<string> { BranchNames.Main, BranchNames.Develop, BranchNames.Stage, BranchNames.Integration, BranchNames.FeatureExample };
            var branchInfos = new List<BranchInfo>();
            
            foreach (var branch in branches)
            {
                var branchType = GetBranchType(branch);
                branchInfos.Add(new BranchInfo
                {
                    BranchName = branch,
                    BranchType = branchType,
                    Description = GetBranchDescription(branch, branchType)
                });
            }
            
            return branchInfos;
        }

        public class ProjectBranchStatus
        {
            public string ProjectName { get; set; } = string.Empty;
            public string CurrentBranch { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public bool HasBranch { get; set; }
        }

        public List<ProjectBranchStatus> GetAllProjectBranchStatus(ChainModel chain)
        {
            var projects = new List<ProjectBranchStatus>();
            
            foreach (var section in chain.Sections)
            {
                var hasBranch = !string.IsNullOrEmpty(section.Branch);
                projects.Add(new ProjectBranchStatus
                {
                    ProjectName = section.Name,
                    CurrentBranch = section.Branch ?? Messages.NoBranch,
                    Status = hasBranch ? Messages.HasBranch : Messages.NoBranch,
                    HasBranch = hasBranch
                });
            }
            
            return projects;
        }

        public int UpdateProjectBranches(ChainModel chain, Dictionary<string, string> projectBranches)
        {
            int updatedCount = 0;
            
            foreach (var kvp in projectBranches)
            {
                var section = chain.Sections.FirstOrDefault(s => s.Name == kvp.Key);
                if (section != null && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    section.Branch = kvp.Value;
                    // Clear tag when setting branch
                    if (section.Properties.ContainsKey(PropertyNames.Tag))
                        section.Properties.Remove(PropertyNames.Tag);
                    updatedCount++;
                }
            }
            
            return updatedCount;
        }

        private string GetBranchType(string branchName)
        {
            if (branchName == BranchNames.Main || branchName == BranchNames.Master) return BranchTypes.Main;
            if (branchName == BranchNames.Develop) return BranchTypes.Development;
            if (branchName.StartsWith(BranchPrefixes.Feature)) return BranchTypes.Feature;
            if (branchName.StartsWith(BranchPrefixes.Hotfix)) return BranchTypes.Hotfix;
            if (branchName.StartsWith(BranchPrefixes.Release)) return BranchTypes.Release;
            if (branchName.StartsWith(BranchPrefixes.Bugfix)) return BranchTypes.Bugfix;
            if (branchName.StartsWith(BranchPrefixes.Personal)) return BranchTypes.Personal;
            if (branchName.StartsWith(BranchPrefixes.Team)) return BranchTypes.Team;
            return BranchTypes.Other;
        }

        private string GetBranchDescription(string branchName, string branchType)
        {
            return branchType switch
            {
                BranchTypes.Main => BranchDescriptions.Main,
                BranchTypes.Development => BranchDescriptions.Development,
                BranchTypes.Feature => BranchDescriptions.Feature,
                BranchTypes.Hotfix => BranchDescriptions.Hotfix,
                BranchTypes.Release => BranchDescriptions.Release,
                BranchTypes.Bugfix => BranchDescriptions.Bugfix,
                BranchTypes.Personal => BranchDescriptions.Personal,
                BranchTypes.Team => BranchDescriptions.Team,
                _ => BranchDescriptions.Custom
            };
        }
    }
    

}