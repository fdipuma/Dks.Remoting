// 2017 Federico Dipuma

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace Dks.Remoting
{
    public class RpcServer
    {
        private readonly RpcOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _tokenSource;
        private NetMQPoller _poller;

        public RpcServer(RpcOptions options, IServiceProvider serviceProvider)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _tokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            Task.Factory.StartNew(Run, _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            _poller.StopAsync();
            _tokenSource.Cancel();
        }

        public void Run()
        {
            using (var receiver = new RouterSocket("@" + _options.ConnectionString))
            using (var queue = new NetMQQueue<NetMQMessage>())
            using (_poller = new NetMQPoller() { queue })
            {
                queue.ReceiveReady += (s, e) =>
                {
                    while (e.Queue.TryDequeue(out NetMQMessage message, TimeSpan.Zero))
                        receiver.SendMultipartMessage(message);
                };

                _poller.RunAsync();

                while (!_tokenSource.IsCancellationRequested)
                {
                    var message = new NetMQMessage(2);
                    if (receiver.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(1000), ref message, 2))
                    {
                        new Thread(() => HandleMessage(queue, message)).Start();
                    }
                }
            }
        }

        private void HandleMessage(NetMQQueue<NetMQMessage> sendQueue, NetMQMessage requestMessage)
        {
            var client = requestMessage[0];
            var requestBytes = requestMessage[1].ToByteArray();

            var requestDto = _options.Serializer.Deserialize<RpcRequestDTO>(requestBytes);
            var response = HandleRequest(requestDto);
            var responseBytes = _options.Serializer.Serialize(response);

            var responseMessage = new NetMQMessage(2);
            responseMessage.Append(client);
            responseMessage.Append(responseBytes);

            sendQueue.Enqueue(responseMessage);
        }

        protected virtual RpcRequest BeginRequest(RpcRequest request)
        {
            return null;
        }

        protected virtual RpcResponse EndRequest(RpcRequest request, RpcResponse response)
        {
            return null;
        }

        private RpcResponse HandleRequest(RpcRequestDTO requestDto)
        {
            var request = new RpcRequest(requestDto);

            _serviceProvider.BeginScope();

            try
            {
                request = BeginRequest(request);

                if (request != null) requestDto = new RpcRequestDTO(request);

                var response = ExecuteRequest(requestDto);

                return EndRequest(request, response) ?? response;
            }
            finally
            {
                _serviceProvider.EndScope();
            }
        }

        private RpcResponse ExecuteRequest(RpcRequestDTO requestDto)
        {
            var serviceType = Type.GetType($"{requestDto.TypeName}, {requestDto.AssemblyName}");
            if (serviceType == null)
            {
                return requestDto.CreateErrorResponse(
                    new InvalidOperationException(
                        $"{requestDto.AssemblyName} {requestDto.TypeName} not registered as service"));
            }

            object service;
            MethodInfo methodInfo;

            try
            {
                service = _serviceProvider.GetServiceInstance(serviceType);
                var implementationType = service.GetType();

                methodInfo = implementationType.GetMethod(requestDto.MethodName,
                    BindingFlags.Public | BindingFlags.Instance);
                if (methodInfo == null)
                {
                    return requestDto.CreateErrorResponse(
                        new InvalidOperationException(
                            $"Method {requestDto.MethodName} not found for {requestDto.AssemblyName} {requestDto.TypeName}"));
                }
            }
            catch (Exception ex)
            {
                return requestDto.CreateErrorResponse(ex);
            }

            var parameters = methodInfo.GetParameters();

            if (parameters.Length != requestDto.Arguments.Count)
            {
                return requestDto.CreateErrorResponse(
                    new ArgumentException(
                        $"Inconsistent number of parameters for method {requestDto.MethodName} of type {requestDto.AssemblyName} {requestDto.TypeName}"));
            }

            var arguments = new List<object>();

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterInfo = parameters[i];
                var paramType = parameterInfo.ParameterType;
                try
                {
                    var argument = requestDto.Arguments[i].ToObject(paramType);
                    arguments.Add(argument);
                }
                catch (Exception ex)
                {
                    return requestDto.CreateErrorResponse(
                        new ArgumentException(
                            $"Invalid parameter type {paramType.Name} of {requestDto.MethodName} of type {requestDto.AssemblyName} {requestDto.TypeName}",
                            ex));
                }
            }

            object returnValue;

            try
            {
                returnValue = methodInfo.Invoke(service, arguments.ToArray());

                if (returnValue is Task t)
                {
                    t.GetAwaiter().GetResult();

                    if (returnValue.GetType().IsGenericType)
                    {
                        var prop = returnValue.GetType().GetProperty("Result");
                        returnValue = prop.GetValue(returnValue);
                    }
                    else
                    {
                        returnValue = null;
                    }
                }

            }
            catch (Exception ex)
            {
                return requestDto.CreateErrorResponse(ex);
            }

            return new RpcResponse(requestDto.Id)
            {
                ReturnValue = returnValue
            };
        }
    }
}