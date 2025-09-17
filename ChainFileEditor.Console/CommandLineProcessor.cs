using ChainFileEditor.Console.Commands;
using ChainFileEditor.Core.Operations;
using ChainFileEditor.Core.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Console
{
    public class CommandLineProcessor
    {
        public CommandLineProcessor()
        {
        }

        public int ProcessArguments(string[] args)
        {
            if (args.Length == 0)
            {
                return 1;
            }

            var arguments = ParseArguments(args);
            
            if (!arguments.ContainsKey("command"))
            {
                return 1;
            }

            var command = arguments["command"].ToLower();

            try
            {
                return command switch
                {
                    "validate" => ExecuteValidate(arguments),
                    "rebase" => ExecuteRebase(arguments),
                    _ => HandleUnknownCommand(command)
                };
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error executing command: {ex.Message}");
                return 2;
            }
        }

        private Dictionary<string, string> ParseArguments(string[] args)
        {
            var arguments = new Dictionary<string, string>();
            
            foreach (var arg in args)
            {
                if (arg.StartsWith("--"))
                {
                    var parts = arg.Substring(2).Split('=', 2);
                    if (parts.Length == 2)
                    {
                        arguments[parts[0]] = parts[1];
                    }
                }
            }
            
            return arguments;
        }

        private int ExecuteValidate(Dictionary<string, string> args)
        {
            if (!args.ContainsKey("chain-file"))
            {
                System.Console.WriteLine("Error: --chain-file parameter is required");
                return 1;
            }

            var command = new ValidateCommand();
            return command.ExecuteAsync(new[] { $"--chain-file={args["chain-file"]}" }).Result;
        }

        private int ExecuteRebase(Dictionary<string, string> args)
        {
            if (!args.ContainsKey("chain-file") || !args.ContainsKey("new-version"))
            {
                System.Console.WriteLine("Error: --chain-file and --new-version parameters are required");
                return 1;
            }

            var command = new RebaseCommand();
            return command.ExecuteAsync(new[] { $"--chain-file={args["chain-file"]}", $"--new-version={args["new-version"]}" }).Result;
        }



        private int HandleUnknownCommand(string command)
        {
            System.Console.WriteLine($"Unknown command: {command}");
            return 1;
        }


    }
}