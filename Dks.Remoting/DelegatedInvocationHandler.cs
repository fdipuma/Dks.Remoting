using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NProxy.Core;

namespace Dks.Remoting
{
    internal class DelegatedInvocationHandler : IInvocationHandler
    {
        private readonly Func<RpcRequest, Task<RpcResponseDTO>> _executeRequestHandler;

        public DelegatedInvocationHandler(Func<RpcRequest, Task<RpcResponseDTO>> executeRequestHandler)
        {
            _executeRequestHandler = executeRequestHandler;
        }

        public object Invoke(object target, MethodInfo methodInfo, object[] parameters)
        {
            var methodName = methodInfo.Name;

            var asyncWrapper = methodInfo.GetCustomAttribute<AsyncWrapperAttribute>();
            if (asyncWrapper != null)
            {
                methodName = asyncWrapper.WrappedMethodName;
            }

            var methodReturnType = methodInfo.ReturnType;

            var request = new RpcRequest(methodInfo.DeclaringType.Assembly.FullName, methodInfo.DeclaringType.FullName, methodName, parameters);
            var task = _executeRequestHandler(request);

            if (methodReturnType == typeof(Task))
            {
                return task;
            }
            
            if (methodReturnType.IsGenericType && methodReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return UnwrapTask(task, methodReturnType);
            }

            var response = task.GetAwaiter().GetResult();

            if (methodReturnType == typeof(void))
            {    
                return null;
            }

            var returnValue = response.ReturnValue.ToObject(methodReturnType);

            return returnValue;
        }

        private static object UnwrapTask(Task<RpcResponseDTO> task, Type genericTaskType)
        {
            var taskReturnType = genericTaskType.GetGenericArguments().First();

            var genericContinuation = ContinuationMethod.MakeGenericMethod(taskReturnType);

            return genericContinuation.Invoke(null, new object[] { task });
        }

        private static readonly MethodInfo ContinuationMethod = typeof(DelegatedInvocationHandler).GetMethod(nameof(SetContinuation), BindingFlags.Static | BindingFlags.NonPublic);

        private static Task<TResult> SetContinuation<TResult>(Task<RpcResponseDTO> task)
        {
            return task.ContinueWith(t => t.Result.ReturnValue.ToObject<TResult>());
        }
    }

    public sealed class AsyncWrapperAttribute : Attribute
    {
        public string WrappedMethodName { get; }

        public AsyncWrapperAttribute(string wrappedMethodName)
        {
            WrappedMethodName = wrappedMethodName;
        }
    }
}