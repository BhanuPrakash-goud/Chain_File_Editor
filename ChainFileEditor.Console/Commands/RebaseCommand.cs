using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Validation;
using ChainFileEditor.Core.Validation.Rules;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChainFileEditor.Console.Commands
{
    public class RebaseCommand : CommandBase
    {
        public override string Name => "rebase";
        public override string Description => "Validates chain file for rebase operation";

        public override async Task<int> ExecuteAsync(string[] args)
        {
            try
            {
                var newVersion = GetArgument(args, "--new-version");
                var chainFile = GetArgument(args, "--chain-file");

                if (string.IsNullOrEmpty(chainFile))
                {
                    System.Console.Write("Enter chain file path: ");
                    chainFile = System.Console.ReadLine();
                }
                
                if (string.IsNullOrEmpty(newVersion))
                {
                    System.Console.Write("Enter new version: ");
                    newVersion = System.Console.ReadLine();
                }

                if (string.IsNullOrEmpty(newVersion) || string.IsNullOrEmpty(chainFile))
                {
                    WriteError("File path and version are required");
                    return 1;
                }

                if (!File.Exists(chainFile))
                {
                    WriteError($"Chain file not found: {chainFile}");
                    return 1;
                }

                var parser = new ChainFileParser();
                var chain = parser.ParsePropertiesFile(chainFile);
                
                var rebaseService = new RebaseService();
                var currentVersion = rebaseService.ExtractCurrentVersion(chain);
                var projectVersions = rebaseService.AnalyzeProjectVersions(chain);
                var selectedProjects = projectVersions.Select(p => p.ProjectName).ToList();
                
                WriteInfo($"Rebasing from version {currentVersion} to {newVersion}");
                WriteInfo($"Found {projectVersions.Count} projects with version properties");
                
                var updatedCount = rebaseService.UpdateSelectedProjects(chain, newVersion, selectedProjects);
                
                var writer = new ChainFileWriter();
                writer.WritePropertiesFile(chainFile, chain);
                
                WriteSuccess($"Rebase completed: {updatedCount} properties updated to version {newVersion}");
                WriteInfo($"File updated: {chainFile}");
                
                return 0;
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
                return 1;
            }
        }

        private string GetArgument(string[] args, string name)
        {
            var arg = args.FirstOrDefault(a => a.StartsWith($"{name}="));
            return arg?.Substring(name.Length + 1) ?? string.Empty;
        }
    }
}