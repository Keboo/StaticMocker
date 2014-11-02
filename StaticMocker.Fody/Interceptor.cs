using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace StaticMocker.Fody
{
    public static class Interceptor
    {
        private static readonly List<IStaticInterceptor> _Interceptors = new List<IStaticInterceptor>();

        internal static void RegisterInterceptor( IStaticInterceptor interceptor )
        {
            _Interceptors.Add( interceptor );
        }

        internal static void UnregisterInterceptor( IStaticInterceptor interceptor )
        {
            _Interceptors.Remove( interceptor );
        }

        public static bool Intercept( MockMethod mockMethod )
        {
            Debug.WriteLine( "Intercepted " + mockMethod );

            return _Interceptors.Any( interceptor => !interceptor.AllowMethodCall( mockMethod ) );
        }
    }
}