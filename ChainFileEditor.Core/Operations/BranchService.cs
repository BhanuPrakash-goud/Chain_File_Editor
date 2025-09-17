using ChainFileEditor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

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
            var branches = new List<string> { "main", "develop", "stage", "integration", "feature/example" };
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
                    CurrentBranch = section.Branch ?? "(no branch)",
                    Status = hasBranch ? "Has Branch" : "No Branch",
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
                    if (section.Properties.ContainsKey("tag"))
                        section.Properties.Remove("tag");
                    updatedCount++;
                }
            }
            
            return updatedCount;
        }

        private string GetBranchType(string branchName)
        {
            if (branchName == "main" || branchName == "master") return "Main";
            if (branchName == "develop") return "Development";
            if (branchName.StartsWith("feature/")) return "Feature";
            if (branchName.StartsWith("hotfix/")) return "Hotfix";
            if (branchName.StartsWith("release/")) return "Release";
            if (branchName.StartsWith("bugfix/")) return "Bugfix";
            if (branchName.StartsWith("personal/")) return "Personal";
            if (branchName.StartsWith("team/")) return "Team";
            return "Other";
        }

        private string GetBranchDescription(string branchName, string branchType)
        {
            return branchType switch
            {
                "Main" => "Production-ready code",
                "Development" => "Integration branch for features",
                "Feature" => "New feature development",
                "Hotfix" => "Critical production fixes",
                "Release" => "Release preparation",
                "Bugfix" => "Bug fixes",
                "Personal" => "Personal development branch",
                "Team" => "Team collaboration branch",
                _ => "Custom branch"
            };
        }
    }
}