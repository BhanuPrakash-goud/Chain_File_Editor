using ChainFileEditor.Wizard;

namespace ChainFileEditor.Wizard
{
    class Program
    {
        static void Main(string[] args)
        {
            var wizardManager = new WizardManager();
            wizardManager.Run();
        }
    }
}
