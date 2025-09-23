namespace ChainFileEditor.Core.Constants
{
    public static class PropertyNames
    {
        public const string Mode = "mode";
        public const string DevMode = "mode.devs";
        public const string Branch = "branch";
        public const string Tag = "tag";
        public const string Fork = "fork";
        public const string TestsUnit = "tests.unit";
    }
    
    public static class ProjectNames
    {
        public const string Content = "content";
        public const string Deployment = "deployment";
        public const string Tests = "tests";
        public const string Designer = "designer";
        public const string DepmService = "depmservice";
        public const string Global = "global";
        public const string GlobalDevs = "global.devs";
    }
    
    public static class BranchNames
    {
        public const string Main = "main";
        public const string Master = "master";
        public const string Develop = "develop";
        public const string Stage = "stage";
        public const string Integration = "integration";
        public const string Dev = "dev";
        public const string FeatureExample = "feature/example";
    }
    
    public static class BranchPrefixes
    {
        public const string DevDepm = "dev/DEPM-";
        public const string Feature = "feature/";
        public const string Hotfix = "hotfix/";
        public const string Release = "release/";
        public const string Bugfix = "bugfix/";
        public const string Personal = "personal/";
        public const string Team = "team/";
    }
    
    public static class ModeValues
    {
        public const string Source = "source";
        public const string Binary = "binary";
        public const string Ignore = "ignore";
    }
    
    public static class DefaultValues
    {
        public const string SourceMode = "source";
        public const string BinaryMode = "binary";
        public const string IgnoreMode = "ignore";
        public const string TrueValue = "true";
        public const string FalseValue = "false";
    }
    
    public static class Messages
    {
        public const string NotSet = "(not set)";
        public const string Clear = "(clear)";
        public const string NoBranch = "(no branch)";
        public const string HasBranch = "Has Branch";
        public const string NotFound = "Not found";
        public const string GlobalVersionProperty = "Global version property";
        public const string GlobalDevVersionProperty = "Global dev version property";
        public const string HasTag = "Has tag";
    }
    
    public static class HeaderComments
    {
        public const string FeatureChainFile = "# Feature chain configuration file";
        public const string Empty = "#";
        public const string JiraSample = "# JIRA: DEPM-123456";
        public const string DescriptionSample = "# Description: Sample";
        public const string JiraPrefix = "# JIRA: DEPM-";
        public const string DescriptionPrefix = "# Description: ";
    }
    
    public static class GlobalPropertyNames
    {
        public const string VersionBinary = "global.version.binary";
        public const string DevVersionBinary = "global.devs.version.binary";
        public const string Recipients = "global.recipients";
    }
    
    public static class ValidationRuleIds
    {
        public const string RequiredProjects = "RequiredProjects";
        public const string ModeRequired = "ModeRequired";
        public const string ModeValidation = "ModeValidation";
        public const string BranchOrTag = "BranchOrTag";
        public const string BranchOrTagRequired = "BranchOrTagRequired";
        public const string ForkValidation = "ForkValidation";
        public const string ContentNotStage = "ContentNotStage";
        public const string TestsPreferBranch = "TestsPreferBranch";
        public const string DevModeOverride = "DevModeOverride";
        public const string GlobalVersionWhenBinary = "GlobalVersionWhenBinary";
        public const string VersionRange = "VersionRange";
        public const string FeatureForkRecommendation = "FeatureForkRecommendation";
        public const string DevModeValidation = "DevModeValidation";
    }
    
    public static class VersionRanges
    {
        public const int MinVersion = 10000;
        public const int MaxVersion = 39999;
        public const int StageMaxVersion = 30000;
    }
    
    public static class RegexPatterns
    {
        public const string ProjectNameExtraction = @"'([^']+)'.*is missing";
    }
    
    public static class BranchTypes
    {
        public const string Main = "Main";
        public const string Development = "Development";
        public const string Feature = "Feature";
        public const string Hotfix = "Hotfix";
        public const string Release = "Release";
        public const string Bugfix = "Bugfix";
        public const string Personal = "Personal";
        public const string Team = "Team";
        public const string Other = "Other";
    }
    
    public static class BranchDescriptions
    {
        public const string Main = "Production-ready code";
        public const string Development = "Integration branch for features";
        public const string Feature = "New feature development";
        public const string Hotfix = "Critical production fixes";
        public const string Release = "Release preparation";
        public const string Bugfix = "Bug fixes";
        public const string Personal = "Personal development branch";
        public const string Team = "Team collaboration branch";
        public const string Custom = "Custom branch";
    }
    
    public static class PropertyTypes
    {
        public const string VersionBinary = "version.binary";
        public const string Tag = "tag";
    }
    
    public static class TagPrefixes
    {
        public const string Build = "Build_";
    }
    
    public static class TagFormats
    {
        public const string BuildPrefix = "Build_12.25.10.";
    }
}