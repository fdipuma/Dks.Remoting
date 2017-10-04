using System;

namespace Dks.Remoting
{
    public interface IServiceProvider
    {
        object GetServiceInstance(Type serviceType);
        void BeginScope();
        void EndScope();
    }
}