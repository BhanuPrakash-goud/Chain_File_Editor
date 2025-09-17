using ChainFileEditor.Core.Models;
using ChainFileEditor.Core.Configuration;
using System.Linq;

namespace ChainFileEditor.Core.Validation.Rules
{
    public class IntegrationTestsRule : ValidationRuleBase
    {
        public override string RuleId => "IntegrationTests";
        public override string Description => "Validates integration test suite configurations";

        public override ValidationResult Validate(ChainModel chain)
        {
            var result = new ValidationResult();
            var config = ConfigurationLoader.LoadValidationConfig();
            var validTestSuites = config.IntegrationTestSuites;

            foreach (var section in chain.Sections)
            {
                foreach (var property in section.Properties)
                {
                    if (property.Key.StartsWith("tests.") && property.Key.EndsWith(".run"))
                    {
                        var testSuiteName = property.Key.Substring(6, property.Key.Length - 10); // Remove "tests." and ".run"
                        
                        if (!validTestSuites.Contains(testSuiteName))
                        {
                            result.AddIssue(CreateWarning($"Unknown integration test suite '{testSuiteName}' in {property.Key}", section.Name));
                        }

                        if (!bool.TryParse(property.Value, out _))
                        {
                            result.AddIssue(CreateError($"Invalid boolean value '{property.Value}' for {property.Key}", section.Name));
                        }
                    }
                }
            }

            return result;
        }
    }
}