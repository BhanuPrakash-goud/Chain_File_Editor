using System;
using System.Threading.Tasks;

namespace ChainFileEditor.Console.Commands
{
    public abstract class CommandBase : ICommand
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        
        public abstract Task<int> ExecuteAsync(string[] args);

        protected void WriteError(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"Error: {message}");
            System.Console.ResetColor();
        }

        protected void WriteSuccess(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine(message);
            System.Console.ResetColor();
        }

        protected void WriteInfo(string message)
        {
            System.Console.WriteLine(message);
        }
    }
}