using System;
using System.Collections.Generic;
using System.Threading;

namespace Dks.Remoting
{
    public sealed class DefaultServiceProvider : IServiceProvider
    {
        private readonly IDictionary<Type, ServiceRegistration> _services;

        private readonly ThreadLocal<IDictionary<Type, object>> _scope = new ThreadLocal<IDictionary<Type, object>>();

        public DefaultServiceProvider()
        {
            _services = new Dictionary<Type, ServiceRegistration>();
        }

        public void RegisterSingleton<TService>(TService instance)
        {
            _services.Add(typeof(TService), new ServiceRegistration(instance));
        }

        public void RegisterScoped<TService, TImplementation>() where TImplementation : TService, new()
        {
            RegisterScoped<TService>(() => new TImplementation());
        }

        public void RegisterScoped<TService>(Func<TService> factory)
        {
            _services.Add(typeof(TService), new ServiceRegistration(factory, true));
        }

        public void RegisterTransient<TService, TImplementation>() where TImplementation : TService, new()
        {
            RegisterTransient<TService>(() => new TImplementation());
        }

        public void RegisterTransient<TService>(Func<TService> factory)
        {
            _services.Add(typeof(TService), new ServiceRegistration(factory, false));
        }

        public object GetServiceInstance(Type serviceType)
        {
            _services.TryGetValue(serviceType, out ServiceRegistration registration);
            if (registration == null)
                throw new InvalidOperationException($"Type {serviceType} is not registered");

            if (registration.IsTransient || registration.IsSingleton)
            {
                return registration.GetInstance();
            }

            if (_scope.Value == null)
                throw new InvalidOperationException("Unable to retrive a scoped instance without any scope started");

            if (!_scope.Value.ContainsKey(serviceType))
                _scope.Value[serviceType] = registration.GetInstance();

            return _scope.Value[serviceType];
        }

        public void BeginScope()
        {
            if (_scope.Value != null)
                throw new InvalidOperationException("Unable to begin a new scope while another is active");
            _scope.Value = new Dictionary<Type, object>();
        }

        public void EndScope()
        {
            if (_scope.Value == null) return;

            foreach (var trackedInstance in _scope.Value.Values)
            {
                if (trackedInstance is IDisposable disposable)
                    disposable.Dispose();
            }

            _scope.Value = null;
        }

        private class ServiceRegistration
        {
            private readonly object _instance;
            private readonly Delegate _factoryDelegate;

            public ServiceRegistration(object instance)
            {
                _instance = instance;
                IsTransient = false;
                IsScoped = false;
            }

            public ServiceRegistration(Delegate factory, bool scoped = false)
            {
                _factoryDelegate = factory;
                IsTransient = !scoped;
                IsScoped = scoped;
            }

            public bool IsTransient { get; }
            public bool IsScoped { get; }
            public bool IsSingleton => !IsScoped && !IsTransient;

            public object GetInstance() => IsSingleton ? _instance : _factoryDelegate.DynamicInvoke();
        }
    }
}