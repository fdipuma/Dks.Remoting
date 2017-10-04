using System;

namespace Dks.Remoting
{
    public interface IRpcSerializer
    {
        byte[] Serialize(object input);

        TObject Deserialize<TObject>(byte[] input);

        object Deserialize(byte[] input, Type objectType);
    }
}