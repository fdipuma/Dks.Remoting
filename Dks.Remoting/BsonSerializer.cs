using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Dks.Remoting
{
    internal class BsonSerializer : IRpcSerializer
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer();

        public byte[] Serialize(object input)
        {
            var ms = new MemoryStream();
            using (var writer = new BsonDataWriter(ms))
            {
                Serializer.Serialize(writer, input);
            }
            return ms.ToArray();
        }

        public TObject Deserialize<TObject>(byte[] input)
        {
            var ms = new MemoryStream(input);
            using (var reader = new BsonDataReader(ms))
            {
                return Serializer.Deserialize<TObject>(reader);
            }
        }

        public object Deserialize(byte[] input, Type objectType)
        {
            var ms = new MemoryStream(input);
            using (var reader = new BsonDataReader(ms))
            {
                return Serializer.Deserialize(reader, objectType);
            }
        }
    }
}