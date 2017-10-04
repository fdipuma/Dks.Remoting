using System;
using Newtonsoft.Json.Linq;

namespace Dks.Remoting
{
    internal sealed class RpcRequestDTO
    {
        public RpcRequestDTO()
        { }

        public RpcRequestDTO(RpcRequest request)
        {
            Id = request.Id;
            AssemblyName = request.AssemblyName;
            TypeName = request.TypeName;
            MethodName = request.MethodName;
            Arguments = JArray.FromObject(request.Arguments);
        }

        public Guid Id { get; set; }
        public string AssemblyName { get; set; }
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public JArray Arguments { get; set; }
    }

    internal static class RpcServerRequestExtensions
    {
        public static RpcResponse CreateErrorResponse(this RpcRequestDTO requestDto, Exception exception)
        {
            return new RpcResponse(requestDto.Id)
            {
                Exception = exception
            };
        }
    }
}