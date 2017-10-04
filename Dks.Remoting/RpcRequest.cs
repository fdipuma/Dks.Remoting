using System;
using System.Reflection;

namespace Dks.Remoting
{
    public sealed class RpcRequest
    {
        public RpcRequest()
        {
            Id = Guid.NewGuid();
        }

        internal RpcRequest(RpcRequestDTO dto)
        {
            Id = dto.Id;
            AssemblyName = dto.AssemblyName;
            TypeName = dto.TypeName;
            MethodName = dto.MethodName;
            Arguments = dto.Arguments?.ToObject<object[]>();
        }

        public RpcRequest(string assemblyName, string typeName, string methodName, object[] arguments = null) : this()
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            MethodName = methodName;
            Arguments = arguments;
        }

        public RpcRequest(MethodInfo methodInfo, object[] arguments = null) : this()
        {
            CheckArguments(methodInfo, arguments);

            AssemblyName = methodInfo.DeclaringType.Assembly.FullName;
            TypeName = methodInfo.DeclaringType.FullName;
            MethodName = methodInfo.Name;
            Arguments = arguments;
        }

        private static void CheckArguments(MethodInfo methodInfo, object[] arguments)
        {
            var parameters = methodInfo.GetParameters();

            if (parameters == null)
                return;

            if (arguments == null || parameters.Length != arguments.Length)
            {
                throw new InvalidOperationException("Invalid number of arguments for provided method");
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                var argument = arguments[i];
                var parameter = parameters[i];

                var parameterType = parameter.ParameterType;

                if (argument == null)
                {
                    if (parameterType.IsClass || parameterType.IsInterface || parameterType.IsArray)
                    {
                        continue;
                    }

                    throw new InvalidOperationException($"Invalid null assignment for parameter {parameter.Name}");
                }

                if (!parameterType.IsInstanceOfType(argument))
                {
                    throw new InvalidOperationException(
                        $"Invalid assignment for parameter {parameter.Name}: cannot convert {argument.GetType()} to {parameterType}");
                }
            }
        }

        public Guid Id { get; set; }
        public string AssemblyName { get; set; }
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public object[] Arguments { get; set; }
    }
}