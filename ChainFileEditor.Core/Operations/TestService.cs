using System;
using System.Collections.Generic;
using System.Linq;
using ChainFileEditor.Core.Models;

namespace ChainFileEditor.Core.Operations
{
    public sealed class TestService
    {
        public class ProjectTestStatus
        {
            public string ProjectName { get; set; } = string.Empty;
            public bool TestsEnabled { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        public List<ProjectTestStatus> GetAllProjectTestStatus(ChainModel chain)
        {
            var projects = new List<ProjectTestStatus>();
            
            foreach (var section in chain.Sections)
            {
                var testsEnabled = section.TestsEnabled || section.TestsUnit;
                projects.Add(new ProjectTestStatus
                {
                    ProjectName = section.Name,
                    TestsEnabled = testsEnabled,
                    Status = testsEnabled ? "Enabled" : "Disabled"
                });
            }
            
            return projects;
        }

        public int UpdateProjectTests(ChainModel chain, Dictionary<string, bool> projectTests)
        {
            int updatedCount = 0;
            
            foreach (var kvp in projectTests)
            {
                var section = chain.Sections.FirstOrDefault(s => s.Name == kvp.Key);
                if (section != null && section.Properties.ContainsKey("tests.unit"))
                {
                    section.TestsUnit = kvp.Value;
                    updatedCount++;
                }
            }
            
            return updatedCount;
        }


    }
}