using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace StaticMocker.Fody
{
    public static class Interceptor
    {
        private const BindingFlags BINDING_FLAGS =
            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly List<IStaticInterceptor> _Interceptors = new List<IStaticInterceptor>();

        internal static void RegisterInterceptor( IStaticInterceptor interceptor )
        {
            _Interceptors.Add( interceptor );
        }

        internal static void UnregisterInterceptor( IStaticInterceptor interceptor )
        {
            _Interceptors.Remove( interceptor );
        }

        public static void Intercept()
        {
            Debug.WriteLine( "Intercept" );
        }

        public static void Intercept( string assemblyName, string typeName, string methodName )
        {
            Debug.WriteLine( "Intercepted {2} {0}.{1}()", typeName, methodName, assemblyName );

            var type = Type.GetType( string.Format("{0}, {1}", typeName, assemblyName), false );
            //TODO: Need to filter by parameters
            var methodInfo = type.GetMethods( BINDING_FLAGS ).SingleOrDefault( x => x.Name == methodName );
            if ( methodInfo == null )
                throw new Exception( "Could not find single method with name " + methodName );

            bool invokeMethod = true;
            foreach ( var interceptor in _Interceptors )
            {
                if ( !interceptor.AllowMethodCall( methodInfo ) )
                {
                    invokeMethod = false;
                }
            }
            if ( invokeMethod )
            {
                try
                {
                    methodInfo.Invoke( null, new object[0] );
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
        }

        public static T Intercept<T>( string assemblyName, string typeName, string methodName )
        {
            Debug.WriteLine( "Intercepted {2} {0}.{1}()", typeName, methodName, assemblyName );

            var type = Type.GetType( string.Format( "{0}, {1}", typeName, assemblyName ), false );
            //TODO: Need to filter by parameters
            var methodInfo = type.GetMethods( BINDING_FLAGS ).SingleOrDefault( x => x.Name == methodName );
            if ( methodInfo == null )
                throw new Exception( "Could not find single method with name " + methodName );

            T rv = default(T);
            bool invokeMethod = true;
            foreach ( var interceptor in _Interceptors )
            {
                object replacement;
                if ( !interceptor.AllowMethodCall( methodInfo, out replacement ) )
                {
                    rv = (T) replacement;
                    invokeMethod = false;
                }
            }
            if ( invokeMethod )
            {
                try
                {
                    return (T)methodInfo.Invoke( null, new object[0] );
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
            return rv;
        }

        //public static bool Intercept<T>(
        //    Type type,
        //    string methodName,
        //    object[] parameters, out T returnValue )
        //{
        //    Debug.WriteLine( "Calling {0}.{1}()", type.FullName, methodName );
        //    returnValue = default( T );
        //    return false;
        //}
    }
}