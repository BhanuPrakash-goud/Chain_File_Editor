using System.Collections.Generic;

namespace ChainFileEditor.Core.Models
{
    public class ChainModel
    {
        public GlobalSection Global { get; set; } = new();
        public List<Section> Sections { get; set; } = new();
        public IntegrationTestsSection IntegrationTests { get; set; } = new();
        public ChainConfiguration Configuration { get; set; } = new();
        public string RawContent { get; set; } = string.Empty;
    }

    public class GlobalSection
    {
        public string Version { get; set; } = string.Empty;
        public string DevsVersion { get; set; } = string.Empty;
        public string VersionBinary { get; set; } = string.Empty;
        public string DevVersionBinary { get; set; } = string.Empty;
        public string Recipients { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string JiraId { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
    }

    public class Section
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, string> Properties { get; set; } = new();
        public bool TestsEnabled { get; set; } = true;
        public bool IsCommented { get; set; } = false;
        

        public string Mode 
        { 
            get => Properties.TryGetValue("mode", out var value) ? value : null;
            set { if (value != null) Properties["mode"] = value; }
        }
        
        public string Branch 
        { 
            get => Properties.TryGetValue("branch", out var value) ? value : null;
            set { if (value != null) Properties["branch"] = value; }
        }
        
        public string Tag 
        { 
            get => Properties.TryGetValue("tag", out var value) ? value : null;
            set { if (value != null) Properties["tag"] = value; }
        }
        
        public string Fork 
        { 
            get => Properties.TryGetValue("fork", out var value) ? value : null;
            set { if (value != null) Properties["fork"] = value; }
        }
        
        public string DevMode 
        { 
            get => Properties.TryGetValue("mode.devs", out var value) ? value : null;
            set { if (value != null) Properties["mode.devs"] = value; }
        }
        
        public bool TestsUnit 
        { 
            get 
            {
                if (Properties.TryGetValue("tests.unit", out var value))
                {
                    return bool.TryParse(value, out var result) && result;
                }
                return false;
            }
            set => Properties["tests.unit"] = value.ToString().ToLower();
        }
    }

    public class IntegrationTestsSection
    {
        public Dictionary<string, bool> TestSuites { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
    }

    public class ChainConfiguration
    {
        public string DefaultMode { get; set; } = "source";
        public bool EnableIntegrationTests { get; set; } = true;
        public string BuildMachineMode { get; set; } = "source";
    }
}