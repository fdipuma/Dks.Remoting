using System;
using System.Threading.Tasks;

namespace Dks.Remoting.Test
{
    public class CustomServiceImpl : ICustomService, IDisposable
    {
        public string GetString(int number)
        {
            return "N° " + number;
        }

        public ExampleDTO GetDto()
        {
            return new ExampleDTO
            {
                IntType = 1,
                LongType = int.MaxValue + 1L,
                StringType = "Hello world"
            };
        }

        public int GetInt(ExampleDTO dto)
        {
            return dto.IntType;
        }

        public async Task<int> GetIntAsync()
        {
            await Task.Delay(3000);
            return 1200;
        }

        public void Dispose()
        {
            Console.WriteLine("DISPOSE CALLED!!");
        }
    }
}