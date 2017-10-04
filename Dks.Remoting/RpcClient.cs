using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using NProxy.Core;

namespace Dks.Remoting
{
    public sealed class RpcClient : IDisposable
    {
        private readonly RpcOptions _options;
        private readonly ConcurrentDictionary<Type, object> _servicesCache = new ConcurrentDictionary<Type, object>();
        private readonly ProxyFactory _proxyFactory;
        private readonly DelegatedInvocationHandler _invocationHandler;
        private readonly CancellationTokenSource _cancellationTokenSource;
        public RpcClient(RpcOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _proxyFactory = new ProxyFactory();
            _invocationHandler = new DelegatedInvocationHandler(ExecuteRequestAsync);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public TService GetServiceProxy<TService>() where TService : class
        {
            return (TService)_servicesCache.GetOrAdd(typeof(TService),
                t => _proxyFactory.CreateProxy<TService>(Type.EmptyTypes, _invocationHandler));
        }

        private async Task<RpcResponseDTO> ExecuteRequestAsync(RpcRequest request)
        {
            var serialized = _options.Serializer.Serialize(request);
            byte[] response = null;

            using (var dealer = new DealerSocket(">" + _options.ConnectionString))
            using (var poller = new NetMQPoller { dealer })
            using (var sem = new SemaphoreSlim(0))
            {
                dealer.ReceiveReady += (s, e) =>
                {
                    response = e.Socket.ReceiveFrameBytes();
                    sem.Release(1);
                };

                poller.RunAsync();

                dealer.SendFrame(serialized);

                await sem.WaitAsync(_cancellationTokenSource.Token);

                poller.Stop();
            }

            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            return _options.Serializer.Deserialize<RpcResponseDTO>(response);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
