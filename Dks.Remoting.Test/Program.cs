using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Dks.Remoting.Test
{
    class Program
    {
        private static RpcOptions _rpcOptions;
        private static RpcClient _client;

        static void Main(string[] args)
        {
            const string connString = "tcp://127.0.0.1:7741";

            _rpcOptions = new RpcOptions(connString);

            //var provider = new DefaultServiceProvider();
            //provider.RegisterScoped<ICustomService, CustomServiceImpl>();

            var container = new Container();
            container.Options.DefaultScopedLifestyle = new ThreadScopedLifestyle();
            container.Register<ICustomService, CustomServiceImpl>(Lifestyle.Scoped);
            container.Register<IBinaryService, BinaryServiceImpl>(Lifestyle.Singleton);
            var provider = new SimpleInjectorServiceProvider(container);

            var server = new RpcServer(_rpcOptions, provider);

            server.Start();

            _client = new RpcClient(_rpcOptions);
            var proxy = _client.GetServiceProxy<ICustomService>();

            //MultipleTests();

            //var client = new RpcClient(_rpcOptions);
            //var proxy = client.GetServiceProxy<ICustomService>();

            //Console.WriteLine(proxy.GetString(14));
            //var dto = new ExampleDTO { IntType = 50, LongType = 344, StringType = "Welcome" };
            //Console.WriteLine(proxy.GetInt(dto));

            //dto = proxy.GetDto();
            //Console.WriteLine($"{dto.IntType}, {dto.LongType}, {dto.StringType}");

            //var proxy2 = client.GetServiceProxy<IBinaryService>();
            //proxy2.PrintUTF8String(Encoding.UTF8.GetBytes("Hello from the other side"));

            GetAsyncExample(proxy).GetAwaiter().GetResult();

            Console.ReadLine();
            server.Stop();
            _client.Dispose();
            Console.WriteLine("Server stopped");
            Console.ReadLine();
        }

        private static async Task GetAsyncExample(ICustomService proxy)
        {
            var r = await proxy.GetIntAsync();
            Console.WriteLine("Received {0}", r);
        }

        private static void MultipleTests()
        {   
            var proxy = _client.GetServiceProxy<ICustomService>();
            foreach (var i in Enumerable.Range(0, 5600))
            {
                var index = i;
                Task.Run(() =>
                {
                    Console.WriteLine($"Sent: {index}. Received: {proxy.GetString(index)}");
                });
            }
        }
    }
}
