using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace StaticMocker.Fody
{
    public static class StaticMock
    {
        public static IStaticMock Create()
        {
            return new StaticMocker();
        }

        private sealed class StaticMocker : IStaticMock, IStaticInterceptor
        {
            private readonly HashSet<MethodInfo> _CalledMethods = new HashSet<MethodInfo>();
            private readonly Dictionary<MethodInfo, StaticMethod> _ExpectedMethodCalls = new Dictionary<MethodInfo, StaticMethod>();

            public StaticMocker()
            {
                Interceptor.RegisterInterceptor( this );
            }

            ~StaticMocker()
            {
                Dispose( false );
            }

            public void Dispose()
            {
                Dispose( true );
                GC.SuppressFinalize( this );
            }

            private void Dispose( bool disposing )
            {
                if ( disposing )
                {
                    Interceptor.UnregisterInterceptor( this );
                }
            }

            public void Verify( Expression<Action> methodExpression )
            {
                Verify( (Expression)methodExpression );
            }

            public void Verify<T>( Expression<Func<T>> methodExpression )
            {
                Verify( (Expression)methodExpression );
            }

            private void Verify( Expression methodExpression )
            {
                var method = GetMethodInfo( methodExpression );
                if ( !_CalledMethods.Contains( method ) )
                {
                    //TODO: need validation exception
                    throw new Exception( string.Format( "{0} was not invoked", method ) );
                }
            }

            public IStaticMethod Expect( Expression<Action> methodExpression )
            {
                var method = GetMethodInfo( methodExpression );
                var rv = new StaticVoidMethod();
                _ExpectedMethodCalls[method] = rv;
                return rv;
            }

            public IStaticMethod<T> Expect<T>( Expression<Func<T>> methodExpression )
            {
                var method = GetMethodInfo( methodExpression );
                var rv = new StaticReturnMethod<T>();
                _ExpectedMethodCalls[method] = rv;
                return rv;
            }

            bool IStaticInterceptor.AllowMethodCall( MethodInfo methodInfo )
            {
                _CalledMethods.Add( methodInfo );
                StaticMethod expected;
                if ( _ExpectedMethodCalls.TryGetValue( methodInfo, out expected ) )
                {
                    expected.InvokeReplacement();
                    return false;
                }
                return true;
            }

            bool IStaticInterceptor.AllowMethodCall( MethodInfo methodInfo, out object replacement )
            {
                _CalledMethods.Add( methodInfo );
                StaticMethod expected;
                if ( _ExpectedMethodCalls.TryGetValue( methodInfo, out expected ) )
                {
                    replacement = expected.InvokeReplacement();
                    return false;
                }
                replacement = null;
                return true;
            }

            private static MethodInfo GetMethodInfo( Expression methodExpression )
            {
                var lambda = methodExpression as LambdaExpression;
                if ( lambda == null )
                    throw new ArgumentException( "Method expression must be a lambda expression to a void static method", "methodExpression" );
                var methodCallExpression = lambda.Body as MethodCallExpression;
                if ( methodCallExpression == null )
                    throw new ArgumentException( "Method expression must be a lambda expression to a void static method", "methodExpression" );
                return methodCallExpression.Method;
            }

            private abstract class StaticMethod
            {
                public abstract object InvokeReplacement();
            }

            private class StaticVoidMethod : StaticMethod, IStaticMethod
            {
                private Action _ReplacementCall;

                public void RatherCall( Action replacement )
                {
                    if (replacement == null) throw new ArgumentNullException("replacement");
                    _ReplacementCall = replacement;
                }

                public override object InvokeReplacement()
                {
                    if (_ReplacementCall != null)
                        _ReplacementCall();
                    return null;
                }
            }

            private class StaticReturnMethod<T> : StaticMethod, IStaticMethod<T>
            {
                private Func<T> _ReplacementCall;
 
                public void RatherCall( Func<T> replacement )
                {
                    if (replacement == null) throw new ArgumentNullException("replacement");
                    _ReplacementCall = replacement;
                }

                public override object InvokeReplacement()
                {
                    if (_ReplacementCall != null)
                        return _ReplacementCall();
                    return null;
                }
            }
        }
    }

    public interface IStaticMock : IDisposable
    {
        void Verify( Expression<Action> methodExpression );
        void Verify<T>( Expression<Func<T>> methodExpression );
        IStaticMethod Expect( Expression<Action> methodExpression );
        IStaticMethod<T> Expect<T>( Expression<Func<T>> methodExpression );
    }

    public interface IStaticMethod
    {
        void RatherCall( Action replacement );
    }

    public interface IStaticMethod<in T>
    {
        void RatherCall( Func<T> replacement );
    }
}