using System;
using System.IO;
using ChainFileEditor.Core.Operations;

namespace ChainFileEditor.Wizard
{
    public class ReorderWizard : WizardBase
    {
        public override string Name => "Chain Reorder";
        public override string Description => "Reorder chain file projects to match template structure";

        public override void Execute()
        {
            ShowHeader("Chain File Reorder Wizard");

            var filePath = PromptForInput("Chain file path", required: true);
            if (filePath == "exit" || !File.Exists(filePath))
            {
                Console.WriteLine("[31m✗ File not found[0m");
                return;
            }

            try
            {
                var parser = new ChainFileParser();
                var chain = parser.ParsePropertiesFile(filePath);

                var reorderService = new ChainReorderService();
                var wasReordered = reorderService.ReorderChain(chain);

                if (wasReordered)
                {
                    var writer = new ChainFileWriter();
                    writer.WritePropertiesFile(filePath, chain);
                    Console.WriteLine("[32m✓ Chain file reordered successfully[0m");
                }
                else
                {
                    Console.WriteLine("[33m! Chain file was already in correct order[0m");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[31m✗ Error: {ex.Message}[0m");
            }
        }
    }
}