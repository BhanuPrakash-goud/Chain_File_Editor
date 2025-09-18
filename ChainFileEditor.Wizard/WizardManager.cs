namespace ChainFileEditor.Wizard
{
    public class WizardManager
    {
        private readonly List<IWizard> _wizards;

        public WizardManager()
        {
            _wizards = new List<IWizard>
            {
                new ValidationWizard(),
                new RebaseWizard(),
                new FeatureChainWizard(),
                new ReorderWizard()
            };
        }

        public void Run()
        {
            while (true)
            {
                ShowMainMenu();
                var choice = GetUserChoice();

                if (choice == 0) // Exit
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                if (choice == 5) // Open GUI
                {
                    try
                    {
                        OpenWinFormsApplication();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error opening GUI: {ex.Message}");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                    }
                }
                else if (choice > 0 && choice <= _wizards.Count)
                {
                    try
                    {
                        _wizards[choice - 1].Execute();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                    }
                }

            }
        }

        private void ShowMainMenu()
        {
            try { Console.Clear(); } catch { }
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║               Chain File Editor Setup Wizard                      ║");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║    validate     - Validate existing chain configuration files     ║");
            Console.WriteLine("║    rebase       - Update version numbers across projects          ║");
            Console.WriteLine("║    create       - Create new feature chain configuration          ║");
            Console.WriteLine("║    reorder      - Reorder projects to match template structure    ║");
            Console.WriteLine("║    gui          - Launch graphical user interface                 ║");
            Console.WriteLine("║    exit         - Exit the wizard                                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.Write("chain-wizard> ");
        }

        private int GetUserChoice()
        {
            while (true)
            {
                var input = Console.ReadLine()?.ToLower().Trim();
                
                if (string.IsNullOrEmpty(input) || input == "validate")
                    return 1; // Default to validation
                    
                if (input == "exit" || input == "quit" || input == "q")
                    return 0; // Exit
                    
                if (input == "rebase" || input == "r")
                    return 2;
                    
                if (input == "create" || input == "c")
                    return 3;
                    
                if (input == "reorder" || input == "o")
                    return 4;
                    
                if (input == "gui" || input == "g")
                    return 5;
                    
                if (int.TryParse(input, out int choice) && choice >= 0 && choice <= 5)
                    return choice;
                
                Console.WriteLine($"Unknown command: '{input}'");
                Console.WriteLine("Valid commands: validate, rebase, create, reorder, gui, exit (or 1-5, 0)");
                Console.Write("chain-wizard> ");
            }
        }
        
        private void OpenWinFormsApplication()
        {
            Console.WriteLine("Opening Chain File Editor GUI...");
            
            var exePath = @"C:\Users\ubhanuprakash\source\repos\Chain_File_Editor\ChainFileEditor.WinForms\bin\Debug\net8.0-windows\ChainFileEditor.WinForms.exe";
            
            if (!File.Exists(exePath))
            {
                Console.WriteLine("GUI application not found. Please build the WinForms project first.");
                Console.WriteLine("Expected location: " + exePath);
                return;
            }
            
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
                Console.WriteLine("GUI application launched successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to launch GUI: {ex.Message}");
            }
        }
    }
}