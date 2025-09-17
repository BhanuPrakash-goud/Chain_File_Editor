# Chain File Editor

A comprehensive .NET application for managing and validating chain configuration files used in deployment pipelines. The application provides both WinForms GUI and Console interfaces for creating, editing, and validating chain files with extensive validation rules and configuration management.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Core Components](#core-components)
- [Validation System](#validation-system)
- [Configuration Management](#configuration-management)
- [User Interfaces](#user-interfaces)
- [Testing Framework](#testing-framework)
- [File Formats](#file-formats)
- [Usage Examples](#usage-examples)
- [Development Guidelines](#development-guidelines)

## Overview

Chain File Editor is designed to manage deployment chain configuration files that define how different projects are built and deployed in the d/EPM platform build system. The application ensures configuration consistency, validates business rules according to the documented build process, and provides tools for creating feature chains and managing project versions.

The editor supports the complete d/EPM build chain ecosystem including master, integration, stage, and feature build chains with proper build number ranges and validation rules.

### Key Features

- **Chain File Validation**: Comprehensive validation with 16+ business rules aligned with d/EPM build process
- **Feature Chain Creation**: GUI-based creation following documented feature chain patterns
- **Build Number Validation**: Validates build numbers against documented ranges (master: 1-9999, integration: 10000-19999, stage: 20000-29999, development: 30000-39999)
- **Integration Test Configuration**: Support for all documented integration test suites
- **Version Management**: Bulk version updates across projects with proper dependency handling
- **Branch/Mode Management**: Project-specific branch and mode configuration following Git branch patterns
- **Fork Management**: Support for feature development in forks with proper validation
- **Configuration-Driven**: JSON-based configuration matching build orchestrator requirements
- **Multi-Interface**: Both WinForms GUI and Console applications
- **Automated Testing**: 50+ unit tests covering all major components
- **Auto-Fix Functionality**: Automatic resolution of common validation issues
- **Project Order Management**: Maintains proper project ordering according to template structure

## Architecture

The application follows a clean architecture pattern with clear separation of concerns:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   WinForms UI   │    │   Console UI    │    │   Wizard UI     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
         ┌─────────────────────────────────────────┐
         │              Core Logic                 │
         │  - Models                               │
         │  - Operations (Parser, Writer, etc.)   │
         │  - Validation (Rules, Summary, etc.)   │
         │  - Configuration                        │
         │  - Auto-Fix Services                    │
         └─────────────────────────────────────────┘
                                 │
         ┌─────────────────────────────────────────┐
         │              Data Layer                 │
         │  - File I/O                             │
         │  - JSON Configuration                   │
         │  - Template Management                  │
         └─────────────────────────────────────────┘
```

## Project Structure

### ChainFileEditor.Core
The core business logic library containing all domain models, operations, and validation logic.

**Key Components:**
- **Models**: ChainModel, Section, GlobalSection, ValidationTypes
- **Operations**: ChainFileParser, ChainFileWriter, FeatureChainService, RebaseService, AutoFixService, ChainReorderService
- **Validation**: ValidationRules, ValidationSummaryGenerator, ValidationAnalyzer, ChainValidator
- **Configuration**: ConfigurationLoader, ValidationRuleFactory

### ChainFileEditor.WinForms
Windows Forms GUI application providing visual interface for chain file management.

**Features:**
- File selection and management
- Validation panel with summary and analysis
- Feature chain creation wizard
- Version rebase functionality
- Branch and mode management
- Test configuration
- Auto-fix integration

### ChainFileEditor.Console
Command-line interface for validation and rebase operations.

**Commands:**
- Validate: Comprehensive chain file validation
- Rebase: Version updates across projects
- Interactive mode with colored output

### ChainFileEditor.Wizard
Interactive console wizard for guided chain file operations.

**Wizards:**
- ValidationWizard: Guided validation process
- RebaseWizard: Interactive version updates
- FeatureChainWizard: Step-by-step feature chain creation
- ReorderWizard: Project reordering to match template

### ChainFileEditor.Tests
Comprehensive test suite with 50+ unit tests covering all major functionality.

**Test Categories:**
- ValidationRulesTests
- ChainFileParserTests
- ChainFileWriterTests
- AutoFixServiceTests
- ConfigurationTests
- Integration Tests

## Core Components

### Models (`ChainFileEditor.Core.Models`)

#### ChainModel
The central data model representing a complete chain configuration.

```csharp
public class ChainModel
{
    public GlobalSection Global { get; set; }           // Global configuration
    public List<Section> Sections { get; set; }         // Project sections
    public IntegrationTestsSection IntegrationTests { get; set; }  // Test configuration
    public ChainConfiguration Configuration { get; set; } // Chain settings
    public string RawContent { get; set; }              // Original file content
}
```

#### Section
Represents individual project configuration within a chain.

```csharp
public class Section
{
    public string Name { get; set; }                    // Project name
    public Dictionary<string, string> Properties { get; set; } // All properties
    public bool TestsEnabled { get; set; }              // Test configuration
    public bool IsCommented { get; set; }               // Comment status
    
    // Direct property accessors
    public string Mode { get; set; }                    // source/binary/ignore
    public string Branch { get; set; }                  // Git branch
    public string Tag { get; set; }                     // Build tag
    public string Fork { get; set; }                    // Fork repository
    public string DevMode { get; set; }                 // Development mode
    public bool TestsUnit { get; set; }                 // Unit tests enabled
}
```

### Operations (`ChainFileEditor.Core.Operations`)

#### ChainFileParser
Converts properties files to ChainModel objects with proper parsing and validation.

#### ChainFileWriter
Converts ChainModel objects back to properties files while maintaining proper project order and structure.

#### FeatureChainService
Creates new feature chain files with proper formatting and business rules compliance.

#### AutoFixService
Automatically resolves validation issues while maintaining proper project order and structure.

**Key Features:**
- Fixes missing projects in correct template order
- Resolves validation rule violations
- Maintains existing valid content
- Applies smart defaults for missing properties

#### ChainReorderService
Reorders chain file sections to match the template structure without affecting valid content.

#### RebaseService
Manages version updates across multiple projects with dependency handling.

## Validation System

The validation system provides comprehensive business rule validation with extensible architecture.

### Core Validation Classes

#### ValidationSummaryGenerator
Generates formatted validation summary reports with professional formatting and comprehensive statistics.

#### ValidationAnalyzer
Provides detailed chain analysis including configuration patterns, risk assessment, and actionable recommendations.

#### ChainValidator
Main validation orchestrator that executes all validation rules and generates comprehensive reports.

### Validation Rules

The system includes 16+ validation rules covering:

1. **ModeRequiredRule**: Ensures every project has a mode specified
2. **ModeValidationRule**: Validates mode values are from allowed set
3. **BranchOrTagRule**: Ensures projects don't have both branch and tag
4. **RequiredProjectsRule**: Validates essential projects are present
5. **ProjectNamingRule**: Validates project names follow conventions
6. **ForkValidationRule**: Validates fork repository references
7. **TestsPreferBranchRule**: Recommends branches over tags for test projects
8. **DevModeOverrideRule**: Warns about development mode overrides
9. **GlobalVersionWhenBinaryRule**: Requires global version when binary mode is used
10. **ContentNotStageRule**: Prevents content project from using stage branch
11. **CommentedOutSectionRule**: Identifies commented configuration sections
12. **FeatureForkRecommendationRule**: Recommends forks for feature branches
13. **VersionConsistencyRule**: Ensures version consistency across projects
14. **BranchOrTagRequiredRule**: Ensures projects have either branch or tag
15. **VersionRangeRule**: Validates version numbers are in correct ranges
16. **GitRepositoryValidationRule**: Validates Git repository accessibility

## Configuration Management

The application uses JSON-based configuration for maintainability and flexibility.

### Configuration Files

#### AppConfig.json
Application-wide settings including default paths, validation settings, and feature flags.

#### ValidationConfig.json
Validation rule configuration with rule enablement, severity levels, and project-specific settings.

### Template Management

The system maintains a strict project order template:
```
framework → repository → olap → modeling → depmservice → consolidation → 
appengine → designer → dashboards → appstudio → officeinteg → 
administration → content → deployment → tests
```

## User Interfaces

### WinForms Application (`ChainFileEditor.WinForms`)

#### MainForm Features
- **File Selection Panel**: Global file selection across all operations
- **Validation Panel**: Comprehensive validation with summary and analysis
- **Feature Chain Panel**: GUI-based feature chain creation
- **Rebase Panel**: Version update management
- **Branch/Mode/Test Panels**: Configuration management
- **Auto-Fix Integration**: One-click issue resolution

### Console Application (`ChainFileEditor.Console`)
Command-line interface with colored output and detailed reporting.

### Wizard Application (`ChainFileEditor.Wizard`)
Interactive console wizard with step-by-step guidance for all operations.

## File Formats

### Properties File Format
Chain files use Java-style properties format with specific ordering requirements:

```properties
# Feature chain configuration file
#
# JIRA: DEPM-123456
# Description: Sample feature chain

global.version.binary=20013
global.devs.version.binary=20013

framework.mode=source
framework.mode.devs=binary
framework.branch=integration
framework.tests.unit=true

repository.mode=source
repository.mode.devs=binary
repository.branch=integration
repository.tests.unit=true

# ... additional projects in template order
```

### Supported Property Types

#### Global Properties
- `global.version.binary`: Global binary version
- `global.devs.version.binary`: Development binary version
- `global.recipients`: Email recipients for notifications

#### Project Properties
- `{project}.mode`: Build mode (source/binary/ignore)
- `{project}.mode.devs`: Development mode override
- `{project}.branch`: Git branch reference
- `{project}.tag`: Build tag reference
- `{project}.fork`: Fork repository (owner/repo format)
- `{project}.tests.unit`: Unit tests enabled (true/false)

## Usage Examples

### Validation Example

```csharp
// Load and validate a chain file
var parser = new ChainFileParser();
var chain = parser.ParsePropertiesFile("example.properties");

var rules = ValidationRuleFactory.CreateAllRules();
var validator = new ChainValidator(rules);
var report = validator.Validate(chain);

// Generate summary and analysis
var summaryGenerator = new ValidationSummaryGenerator();
var analyzer = new ValidationAnalyzer();

var summary = summaryGenerator.GenerateSummary(report, "example.properties");
var analysis = analyzer.GenerateAnalysis(report, chain);
```

### Auto-Fix Example

```csharp
// Auto-fix validation issues
var autoFixService = new AutoFixService();
var fixableIssues = report.Issues.Where(i => i.IsAutoFixable).ToList();
var fixedCount = autoFixService.ApplyAutoFixes(chain, fixableIssues);

// Save the fixed chain
var writer = new ChainFileWriter();
writer.WritePropertiesFile("example.properties", chain);
```

### Feature Chain Creation Example

```csharp
// Create a new feature chain using the wizard
var wizard = new FeatureChainWizard();
wizard.Execute(); // Interactive creation process
```

## Development Guidelines

### Code Organization
- **Separation of Concerns**: Clear boundaries between UI, business logic, and data
- **Single Responsibility**: Each class has one primary responsibility
- **Template Compliance**: All operations maintain proper project ordering
- **Configuration-Driven**: Avoid hardcoded values

### Error Handling
- **Graceful Degradation**: Application continues working when non-critical errors occur
- **User-Friendly Messages**: Technical errors translated to user-understandable messages
- **Comprehensive Logging**: Detailed logging for debugging and monitoring

### Testing Strategy
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions
- **Template Compliance Tests**: Verify project ordering is maintained
- **Auto-Fix Tests**: Validate automatic issue resolution

### Performance Considerations
- **Lazy Loading**: Load data only when needed
- **Caching**: Cache frequently accessed configuration
- **Async Operations**: Use async/await for I/O operations
- **Memory Management**: Proper disposal of resources

## Getting Started

### Prerequisites
- .NET 8.0 or later
- Windows OS (for WinForms interface)
- Visual Studio 2022 or VS Code

### Building the Project
```bash
# Clone the repository
git clone <repository-url>
cd Chain_File_Editor

# Build all projects
dotnet build

# Run tests
dotnet test

# Run console application
dotnet run --project ChainFileEditor.Console

# Run wizard
dotnet run --project ChainFileEditor.Wizard
```

### Configuration
1. Update `AppConfig.json` with your default chain file paths
2. Customize `ValidationConfig.json` for your validation requirements
3. Modify project templates in configuration files as needed

## Contributing

1. Follow the established architecture patterns
2. Maintain template compliance in all operations
3. Add comprehensive tests for new features
4. Update documentation for significant changes
5. Ensure auto-fix functionality works with new validation rules

## License

This project is licensed under the MIT License - see the LICENSE file for details.