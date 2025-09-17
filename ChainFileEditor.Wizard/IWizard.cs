namespace ChainFileEditor.Wizard
{
    public interface IWizard
    {
        string Name { get; }
        string Description { get; }
        void Execute();
    }

    public abstract class WizardBase : IWizard
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract void Execute();

        protected string PromptForInput(string message, string defaultValue = "", bool required = false)
        {
            while (true)
            {
                Console.Write($"[33m▶[0m {message}");
                if (!string.IsNullOrEmpty(defaultValue))
                    Console.Write($" [90m[{defaultValue}][0m");
                Console.Write(": ");
                
                var input = Console.ReadLine();
                if (input?.ToLower() == "exit") return "exit";
                
                var result = string.IsNullOrEmpty(input) ? defaultValue : input;
                
                if (required && string.IsNullOrWhiteSpace(result))
                {
                    Console.WriteLine("[31m✗ This field is required. Please enter a value.[0m");
                    continue;
                }
                
                Console.WriteLine($"[32m✓[0m Set to: [36m{result}[0m");
                return result;
            }
        }

        protected bool PromptForConfirmation(string message)
        {
            Console.Write($"{message} (y/n): ");
            var response = Console.ReadLine()?.ToLower();
            return response == "y" || response == "yes";
        }

        protected int PromptForChoice(string message, string[] options)
        {
            Console.WriteLine(message);
            for (int i = 0; i < options.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {options[i]}");
            }
            
            while (true)
            {
                Console.Write("Select option (1-" + options.Length + "): ");
                var input = Console.ReadLine();
                
                if (input?.ToLower() == "exit") return -1;
                
                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= options.Length)
                {
                    return choice - 1;
                }
                
                Console.WriteLine($"Invalid choice '{input}'. Please enter a number between 1 and {options.Length}.");
            }
        }

        protected void ShowHeader(string title)
        {
            try { Console.Clear(); } catch { }
            Console.WriteLine("╔═════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  [36m{title}[0m".PadRight(85) + "║");
            Console.WriteLine("╚═════════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
        }
    }
}