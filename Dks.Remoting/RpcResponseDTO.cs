using System;
using Newtonsoft.Json.Linq;

namespace Dks.Remoting
{
    internal sealed class RpcResponseDTO
    {
        public Guid Id { get; set; }
        public JToken ReturnValue { get; set; }
        public Exception Exception { get; set; }
    }
}