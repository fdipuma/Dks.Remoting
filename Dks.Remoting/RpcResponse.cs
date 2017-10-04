using System;

namespace Dks.Remoting
{
    public sealed class RpcResponse
    {
        public RpcResponse(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
        public object ReturnValue { get; set; }
        public Exception Exception { get; set; }
    }
}