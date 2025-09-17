using System.Threading.Tasks;

namespace ChainFileEditor.Console.Commands
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        Task<int> ExecuteAsync(string[] args);
    }


}