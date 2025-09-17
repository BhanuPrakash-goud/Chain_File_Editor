using ChainFileEditor.Console.Commands;
using System;
using System.Threading.Tasks;

namespace ChainFileEditor.Console
{
    class Program
    {
        static async Task<int> Main(string[] args)
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
                        "1" => "validate",
                        "2" => "rebase",
                        _ => null
                    };
                    
                    if (commandName == null)
                    {
                        return 1;
                    }
                }
                else
                {
                    commandName = GetCommand(args);
                    
                    if (string.IsNullOrEmpty(commandName))
                    {
                        return 1;
                    }
                }

                var command = CreateCommand(commandName);
                if (command == null)
                {
                    System.Console.WriteLine($"Error: Unknown command '{commandName}'");
                    return 1;
                }

                return await command.ExecuteAsync(args);
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"Fatal error: {ex.Message}");
                System.Console.ResetColor();
                return 1;
            }
        }

        private static string GetCommand(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("--command="))
                {
                    return arg.Substring("--command=".Length);
                }
            }
            return null;
        }

        private static ICommand CreateCommand(string commandName)
        {
            return commandName.ToLower() switch
            {
                "rebase" => new RebaseCommand(),
                "validate" => new ValidateCommand(),
                _ => null
            };
        }


    }
}