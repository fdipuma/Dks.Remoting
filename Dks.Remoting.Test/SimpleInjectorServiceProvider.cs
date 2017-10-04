using System;
using System.Threading;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Dks.Remoting.Test
{
    public class SimpleInjectorServiceProvider : IServiceProvider
    {
        private readonly Container _container;
        private readonly ThreadLocal<Scope> _currentScope = new ThreadLocal<Scope>();

        public SimpleInjectorServiceProvider(Container container)
        {
            _container = container;
        }
        public object GetServiceInstance(Type serviceType)
        {
            return _container.GetInstance(serviceType);
        }

        public void BeginScope()
        {
            if (_currentScope.Value != null)
                throw new InvalidOperationException("Scope already begun");

            _currentScope.Value = ThreadScopedLifestyle.BeginScope(_container);
        }

        public void EndScope()
        {
            _currentScope.Value?.Dispose();
            _currentScope.Value = null;
        }
    }
}