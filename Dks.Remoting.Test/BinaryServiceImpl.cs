using System;
using System.Text;

namespace Dks.Remoting.Test
{
    public class BinaryServiceImpl : IBinaryService
    {
        public void PrintUTF8String(byte[] array)
        {
            Console.WriteLine(Encoding.UTF8.GetString(array));
        }
    }
}