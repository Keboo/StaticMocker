using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StaticMocker.Fody
{
    public class MockMethod
    {
        private const BindingFlags BINDING_FLAGS =
            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public string AssemblyName { get; private set; }
        public string TypeName { get; private set; }
        public string MethodName { get; private set; }
        public IList<Param> Parameters { get; private set; }

        public MockMethod( string assemblyName, string typeName, string methodName, params Param[] parameters )
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            MethodName = methodName;
            Parameters = parameters ?? new Param[0];
        }

        public T GetOutValue<T>( string parameterName )
        {
            var @param = Parameters.First( x => x.Name == parameterName );
            return (T)Convert.ChangeType( param.Value, typeof( T ) );
        }

        public MethodInfo GetMethodInfo()
        {
            var type = Type.GetType( string.Format( "{0}, {1}", TypeName, AssemblyName ), false );

            return (from method in type.GetMethods(BINDING_FLAGS)
                where method.Name == MethodName
                let paramTypes = method.GetParameters().Select(x => x.ParameterType).ToArray()
                where Parameters.Select(x => x.Type).SequenceEqual(paramTypes)
                select method).Single();
        }

        public object ReturnValue { get; set; }

        public override string ToString()
        {
            return string.Format( "{0}.{1}()", TypeName, MethodName );
        }
    }
}