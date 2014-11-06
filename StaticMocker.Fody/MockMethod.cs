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

        public MockMethod( MethodInfo methodInfo, IList<Param> parameters )
        {
            if ( methodInfo == null ) throw new ArgumentNullException( "methodInfo" );

            if ( methodInfo.DeclaringType != null )
            {
                AssemblyName = methodInfo.DeclaringType.Assembly.FullName;
                TypeName = methodInfo.DeclaringType.FullName;
            }
            MethodName = methodInfo.Name;
            Parameters = parameters ?? new Param[0];
        }

        public T GetOutValue<T>( string parameterName )
        {
            //TODO: Better exception if the parameter is not found
            var @param = Parameters.First( x => x.Name == parameterName );
            return (T)Convert.ChangeType( param.Value ?? default( T ), typeof( T ) );
        }

        public MethodInfo GetMethodInfo()
        {
            var type = Type.GetType( string.Format( "{0}, {1}", TypeName, AssemblyName ), false );

            var methodInfo = ( from method in type.GetMethods( BINDING_FLAGS )
                               where method.Name == MethodName
                               let paramTypes = method.GetParameters().Select(
                                   x => x.IsOut
                                       ? x.ParameterType.GetElementType()
                                       : x.ParameterType )
                                   .ToArray()
                               where Parameters.Select( x => x.Type ).SequenceEqual( paramTypes )
                               select method ).Single();

            return methodInfo;
        }

        public object ReturnValue { get; set; }

        public override string ToString()
        {
            return string.Format( "{0}.{1}({2})", TypeName, MethodName,
                string.Join( ", ", Parameters.Select( x => string.Format( "{0} {1}", x.Type, x.Name ) ) ) );
        }

        public bool IsMatch( MockMethod other )
        {
            if ( string.Equals( AssemblyName, other.AssemblyName ) &&
                string.Equals( TypeName, other.TypeName ) &&
                string.Equals( MethodName, other.MethodName ) &&
                Parameters.Count == other.Parameters.Count )
            {
                for ( int i = 0; i < Parameters.Count; i++ )
                {
                    if ( !Parameters[i].Matches( other.Parameters[i] ) )
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public override bool Equals( object obj )
        {
            if ( ReferenceEquals( null, obj ) ) return false;
            if ( ReferenceEquals( this, obj ) ) return true;
            if ( obj.GetType() != GetType() ) return false;
            return Equals( (MockMethod)obj );
        }

        protected bool Equals( MockMethod other )
        {
            return string.Equals( AssemblyName, other.AssemblyName ) &&
                   string.Equals( TypeName, other.TypeName ) &&
                   string.Equals( MethodName, other.MethodName ) &&
                   ParametersEquals( other.Parameters );
        }

        private bool ParametersEquals( IEnumerable<Param> otherParams )
        {
            if ( ReferenceEquals( null, Parameters ) && ReferenceEquals( null, otherParams ) )
                return true;
            if ( ReferenceEquals( null, Parameters ) || ReferenceEquals( null, otherParams ) )
                return false;
            return Parameters.SequenceEqual( otherParams );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ( AssemblyName != null ? AssemblyName.GetHashCode() : 0 );
                hashCode = ( hashCode * 397 ) ^ ( TypeName != null ? TypeName.GetHashCode() : 0 );
                hashCode = ( hashCode * 397 ) ^ ( MethodName != null ? MethodName.GetHashCode() : 0 );
                hashCode = ( hashCode * 397 ) ^ ( Parameters != null ? Parameters.Aggregate( 0, ( value, param ) => ( value * 397 ) ^ param.GetHashCode() ) : 0 );
                return hashCode;
            }
        }
    }
}