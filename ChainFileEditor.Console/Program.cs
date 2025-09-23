using ChainFileEditor.Console.Commands;
using System;
using System.Threading.Tasks;

namespace ChainFileEditor.Console
{
    internal static class Program
    {
        private const string ValidateCommand = "validate";
        private const string RebaseCommand = "rebase";
        private const string CommandPrefix = "--command=";
        private const int SuccessExitCode = 0;
        private const int ErrorExitCode = 1;
        private static async Task<int> Main(string[] args)
        {
            try
            {
                string commandName;
                
                if (args.Length == 0)
                {
                    System.Console.WriteLine("1. Validate");
                    System.Console.WriteLine("2. Rebase");
                    System.Console.Write("Choose operation: ");
                    var choice = System.Console.ReadLine();
                    
                    commandName = choice switch
                    {
                        "1" => ValidateCommand,
                        "2" => RebaseCommand,
                        _ => null
                    };
                    
                    if (commandName == null)
                    {
                        return ErrorExitCode;
                    }
                }
                else
                {
                    commandName = GetCommand(args);
                    
                    if (string.IsNullOrEmpty(commandName))
                    {
                        return ErrorExitCode;
                    }
                }

                var command = CreateCommand(commandName);
                if (command == null)
                {
                    System.Console.WriteLine($"Error: Unknown command '{commandName}'");
                    return ErrorExitCode;
                }

                return await command.ExecuteAsync(args);
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"Fatal error: {ex.Message}");
                System.Console.ResetColor();
                return ErrorExitCode;
            }
        }

        private static string GetCommand(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith(CommandPrefix))
                {
                    return arg.Substring(CommandPrefix.Length);
                }
            }
            return null;
        }

        private static ICommand CreateCommand(string commandName)
        {
            return commandName.ToLower() switch
            {
                RebaseCommand => new Commands.RebaseCommand(),
                ValidateCommand => new Commands.ValidateCommand(),
                _ => null
            };
        }


    }
}