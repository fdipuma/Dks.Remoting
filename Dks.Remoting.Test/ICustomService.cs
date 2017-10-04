using System.Threading.Tasks;

namespace Dks.Remoting.Test
{
    public interface ICustomService
    {
        string GetString(int number);
        ExampleDTO GetDto();
        int GetInt(ExampleDTO dto);
        Task<int> GetIntAsync();
    }
}