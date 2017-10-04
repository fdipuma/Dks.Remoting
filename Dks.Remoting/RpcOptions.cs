using System;

namespace Dks.Remoting
{
    public sealed class RpcOptions
    {
        public RpcOptions(string connectionString) : this(connectionString,new BsonSerializer())
        { }

        public RpcOptions(string connectionString, IRpcSerializer serializer)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public string ConnectionString { get; }
        public IRpcSerializer Serializer { get; }
    }
}