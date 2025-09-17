using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ChainFileEditor.Core.Configuration
{
    public class ValidationRuleConfig
    {
        public string RuleId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public ChainFileEditor.Core.Validation.ValidationSeverity Severity { get; set; }
        public string RuleType { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    public class ValidationConfiguration
    {
        public ValidationSettings ValidationSettings { get; set; } = new();
        public RulesConfig Rules { get; set; } = new();
        public Dictionary<string, List<string>> KnownForks { get; set; } = new();
        public List<string> MainRepositories { get; set; } = new();
        public List<string> AllProjects { get; set; } = new();
        public List<string> IntegrationTestSuites { get; set; } = new();
        public string GitBaseUrl { get; set; } = string.Empty;
        public List<ValidationRuleConfig> ValidationRules { get; set; } = new();
        public string ForkPattern { get; set; } = string.Empty;
        public string ProjectNamingPattern { get; set; } = string.Empty;
    }

    public class ValidationSettings
    {
        public List<string> ValidModes { get; set; } = new();
        public List<string> ValidBooleans { get; set; } = new();
        public List<string> AllValidProperties { get; set; } = new();
        public List<string> ValidGlobalProperties { get; set; } = new();
        public List<string> RequiredProjects { get; set; } = new();
        public List<string> RequiredProperties { get; set; } = new();
        public Dictionary<string, List<string>> RestrictedBranches { get; set; } = new();
        public string FeatureBranchPattern { get; set; } = string.Empty;
        public string CommentPrefix { get; set; } = string.Empty;
        public string MainFork { get; set; } = string.Empty;
        public string TestSuitePattern { get; set; } = string.Empty;
        public string TagPattern { get; set; } = string.Empty;
        public List<string> ForkPatterns { get; set; } = new();
        public List<string> VersionPatterns { get; set; } = new();
        public string RepositoryBasePath { get; set; } = string.Empty;
        public string RepositoryBaseUrl { get; set; } = string.Empty;
        public bool EnableAutoCloning { get; set; }
        public string ForkRepositoryBasePath { get; set; } = string.Empty;
        public string DefaultBuildMode { get; set; } = string.Empty;
        public string GlobalSection { get; set; } = string.Empty;
        public string TestsSection { get; set; } = string.Empty;
        public List<string> ExcludedProjectPatterns { get; set; } = new();
        public List<string> OptionalProjects { get; set; } = new();
        public VersionRangeConfig VersionRange { get; set; } = new();
        public Dictionary<string, BuildNumberRange> BuildNumberRanges { get; set; } = new();
    }

    public class VersionRangeConfig
    {
        public int MinVersion { get; set; } = 10000;
        public int MaxVersion { get; set; } = 30000;
    }

    public class BuildNumberRange
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class RulesConfig
    {
        public BranchOrTagRuleConfig BranchOrTagRule { get; set; } = new();
        public ModeValidationRuleConfig ModeValidationRule { get; set; } = new();
        public GlobalVersionRuleConfig GlobalVersionRule { get; set; } = new();
        public ContentProjectRuleConfig ContentProjectRule { get; set; } = new();
        public TestsProjectRuleConfig TestsProjectRule { get; set; } = new();
        public FeatureBranchForkRuleConfig FeatureBranchForkRule { get; set; } = new();
        public ForkValidationRuleConfig ForkValidationRule { get; set; } = new();
        public TagValidationRuleConfig TagValidationRule { get; set; } = new();
        public BranchValidationRuleConfig BranchValidationRule { get; set; } = new();
        public BooleanValidationRuleConfig BooleanValidationRule { get; set; } = new();
        public VersionValidationRuleConfig VersionValidationRule { get; set; } = new();
        public AllProjectsSectionRuleConfig AllProjectsSectionRule { get; set; } = new();
        public RequiredSectionsRuleConfig RequiredSectionsRule { get; set; } = new();
    }

    public class BranchOrTagRuleConfig
    {
        public bool Enabled { get; set; }
        public Dictionary<string, string> ErrorMessages { get; set; } = new();
        public string BranchProperty { get; set; } = string.Empty;
        public string TagProperty { get; set; } = string.Empty;
    }

    public class ModeValidationRuleConfig
    {
        public bool Enabled { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ApplicableProperties { get; set; } = new();
    }

    public class GlobalVersionRuleConfig
    {
        public bool Enabled { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ModeProperties { get; set; } = new();
        public string BinaryModeValue { get; set; } = string.Empty;
        public List<string> GlobalVersionProperties { get; set; } = new();
    }

    public class ContentProjectRuleConfig
    {
        public bool Enabled { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ApplicableProperty { get; set; } = string.Empty;
    }

    public class TestsProjectRuleConfig
    {
        public bool Enabled { get; set; }
        public string WarningMessage { get; set; } = string.Empty;
        public string ApplicableProperty { get; set; } = string.Empty;
    }

    public class FeatureBranchForkRuleConfig
    {
        public bool Enabled { get; set; }
        public string WarningMessage { get; set; } = string.Empty;
        public string BranchProperty { get; set; } = string.Empty;
        public string ForkProperty { get; set; } = string.Empty;
    }

    public class ForkValidationRuleConfig
    {
        public bool Enabled { get; set; }
        public Dictionary<string, string> ErrorMessages { get; set; } = new();
        public string ApplicableProperty { get; set; } = string.Empty;
        public string ForkSeparator { get; set; } = string.Empty;
    }

    public class TagValidationRuleConfig
    {
        public bool Enabled { get; set; }
        public Dictionary<string, string> ErrorMessages { get; set; } = new();
        public string ApplicableProperty { get; set; } = string.Empty;
        public string EnableGitValidation { get; set; } = string.Empty;
    }

    public class BranchValidationRuleConfig
    {
        public bool Enabled { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ApplicableProperty { get; set; } = string.Empty;
        public string EnableGitValidation { get; set; } = string.Empty;
    }

    public class BooleanValidationRuleConfig
    {
        public bool Enabled { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ApplicableProperties { get; set; } = new();
    }

    public class VersionValidationRuleConfig
    {
        public bool Enabled { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ApplicableProperties { get; set; } = new();
        public string RegexPrefix { get; set; } = string.Empty;
    }

    public class AllProjectsSectionRuleConfig
    {
        public bool Enabled { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string KeyValueSeparator { get; set; } = string.Empty;
        public string PropertySeparator { get; set; } = string.Empty;
    }

    public class RequiredSectionsRuleConfig
    {
        public bool Enabled { get; set; }
        public Dictionary<string, string> ErrorMessages { get; set; } = new();
        public List<string> BranchOrTagProperties { get; set; } = new();
    }

    public class WindowSettings
    {
        public string WindowState { get; set; } = string.Empty;
        public string StartPosition { get; set; } = string.Empty;
        public string FormBorderStyle { get; set; } = string.Empty;
        public SizeConfig MinimumSize { get; set; } = new();
    }

    public class SizeConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class ColorConfig
    {
        public string Background { get; set; } = string.Empty;
        public string Primary { get; set; } = string.Empty;
        public string Success { get; set; } = string.Empty;
        public string Warning { get; set; } = string.Empty;
        public string Danger { get; set; } = string.Empty;
        public string Secondary { get; set; } = string.Empty;
        public string Purple { get; set; } = string.Empty;
    }

    public class UIConfig
    {
        public WindowSettings WindowSettings { get; set; } = new();
        public ColorConfig Colors { get; set; } = new();
    }

    public class FileFiltersConfig
    {
        public string ChainFiles { get; set; } = string.Empty;
        public string PropertiesFiles { get; set; } = string.Empty;
    }

    public class DefaultPathsConfig
    {
        public string WorkingDirectory { get; set; } = string.Empty;
    }

    public class AppConfiguration
    {
        public UIConfig UI { get; set; } = new();
        public FileFiltersConfig FileFilters { get; set; } = new();
        public DefaultPathsConfig DefaultPaths { get; set; } = new();
    }

    public static class ConfigurationLoader
    {
        private static ValidationConfiguration? _cachedValidationConfig;
        private static readonly object _lock = new();

        public static ValidationConfiguration LoadValidationConfig()
        {
            if (_cachedValidationConfig != null)
                return _cachedValidationConfig;

            lock (_lock)
            {
                if (_cachedValidationConfig != null)
                    return _cachedValidationConfig;

                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "ValidationConfig.json");
                
                if (!File.Exists(configPath))
                {
                    _cachedValidationConfig = CreateDefaultValidationConfiguration();
                    return _cachedValidationConfig;
                }

                try
                {
                    var json = File.ReadAllText(configPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };
                    
                    _cachedValidationConfig = JsonSerializer.Deserialize<ValidationConfiguration>(json, options) ?? CreateDefaultValidationConfiguration();
                    return _cachedValidationConfig;
                }
                catch (Exception)
                {
                    _cachedValidationConfig = CreateDefaultValidationConfiguration();
                    return _cachedValidationConfig;
                }
            }
        }

        private static ValidationConfiguration CreateDefaultValidationConfiguration()
        {
            return new ValidationConfiguration
            {
                ValidationSettings = new ValidationSettings
                {
                    ValidModes = ["source", "binary", "ignore"],
                    RequiredProjects = ["framework", "repository", "appengine"],
                    BuildNumberRanges = new Dictionary<string, BuildNumberRange>
                    {
                        ["master"] = new BuildNumberRange { Min = 1, Max = 9999 },
                        ["integration"] = new BuildNumberRange { Min = 10000, Max = 19999 },
                        ["stage"] = new BuildNumberRange { Min = 20000, Max = 29999 },
                        ["development"] = new BuildNumberRange { Min = 30000, Max = 39999 },
                        ["local"] = new BuildNumberRange { Min = 50000, Max = 99999 }
                    }
                },
                MainRepositories = ["framework", "repository", "olap", "modeling", "depmservice", 
                    "consolidation", "appengine", "designer", "dashboards", "appstudio", 
                    "officeinteg", "administration", "content", "deployment"],
                AllProjects = ["framework", "repository", "olap", "modeling", "depmservice", 
                    "consolidation", "appengine", "designer", "dashboards", "appstudio", 
                    "officeinteg", "administration", "content", "deployment"],
                IntegrationTestSuites = ["AdhocWidget", "AdministrationService", "AppEngineService", 
                    "AppsProvisioning", "AppStudioService", "BusinessModelingServiceSet1", 
                    "BusinessModelingServiceSet2", "ConsolidationService", "ContentIntegration", 
                    "DashboardsService", "dEPMAppsUpdate", "dEPMRegressionSet1", "dEPMRegressionSet2", 
                    "dEPMSmoke", "EPMWorkflow", "FarmCreation", "FarmUpgrade", "MultiFarm", 
                    "OfficeIntegrationService", "OlapService", "OlapAPI", "SelfService", 
                    "TenantClone", "WorkforceBudgetingSet1", "WorkforceBudgetingSet2"]
            };
        }

        public static void ClearValidationConfigCache()
        {
            lock (_lock)
            {
                _cachedValidationConfig = null;
            }
        }

        public static AppConfiguration LoadAppConfig()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "AppConfig.json");
            if (!File.Exists(configPath))
                return new AppConfiguration();

            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<AppConfiguration>(json) ?? new AppConfiguration();
        }
    }
}