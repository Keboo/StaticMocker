using System;
using System.Collections.Generic;
using System.Linq;
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
            private readonly HashSet<MockMethod> _CalledMethods = new HashSet<MockMethod>();
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
                var mockMehod = GetMockMethod( methodExpression );

                if ( !_CalledMethods.Contains( mockMehod ) )
                {
                    throw new StaticMockVerificationException( string.Format( "{0} was not invoked", mockMehod ) );
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

            bool IStaticInterceptor.AllowMethodCall( MockMethod mockMethod )
            {
                var methodInfo = mockMethod.GetMethodInfo();

                _CalledMethods.Add( mockMethod );
                StaticMethod staticMethod;
                if ( _ExpectedMethodCalls.TryGetValue( methodInfo, out staticMethod ) )
                {
                    staticMethod.Handle( mockMethod );
                    return false;
                }
                return true;
            }

            private static MethodInfo GetMethodInfo( Expression methodExpression )
            {
                MethodCallExpression methodCallExpression = GetMethodExpression( methodExpression );
                return methodCallExpression.Method;
            }

            private static MockMethod GetMockMethod( Expression expression )
            {
                var methodExpression = GetMethodExpression( expression );
                var methodInfo = methodExpression.Method;
                foreach ( ParameterInfo parameter in methodInfo.GetParameters() )
                {

                }
                return new MockMethod( methodInfo, new Param[0] );
            }

            private static IList<Param> GetMethodParameters( Expression methodExpression )
            {
                MethodCallExpression methodCallExpression = GetMethodExpression( methodExpression );
                //return methodCallExpression.Arguments.Select(x =>
                //{
                //    
                //}).ToArray();
                return null;
            }

            private static MethodCallExpression GetMethodExpression( Expression methodExpression )
            {
                var lambda = methodExpression as LambdaExpression;
                if ( lambda == null )
                    throw new ArgumentException( "Method expression must be a lambda expression to a void static method", "methodExpression" );
                var methodCallExpression = lambda.Body as MethodCallExpression;
                if ( methodCallExpression == null )
                    throw new ArgumentException( "Method expression must be a lambda expression to a void static method", "methodExpression" );
                return methodCallExpression;
            }

            private abstract class StaticMethod
            {
                public abstract void Handle( MockMethod mockMethod );
            }

            private class StaticVoidMethod : StaticMethod, IStaticMethod
            {
                private Action _ReplacementCall;

                public void RatherCall( Action replacement )
                {
                    if ( replacement == null ) throw new ArgumentNullException( "replacement" );
                    _ReplacementCall = replacement;
                }

                public override void Handle( MockMethod mockMethod )
                {
                    if ( _ReplacementCall != null )
                        _ReplacementCall();
                    //TODO: Out parameters
                }
            }

            private class StaticReturnMethod<T> : StaticMethod, IStaticMethod<T>
            {
                private Func<T> _ReplacementCall;

                public void RatherCall( Func<T> replacement )
                {
                    if ( replacement == null ) throw new ArgumentNullException( "replacement" );
                    _ReplacementCall = replacement;
                }

                public override void Handle( MockMethod mockMethod )
                {
                    if ( _ReplacementCall != null )
                    {
                        mockMethod.ReturnValue = _ReplacementCall();
                    }
                    //TODO: Out parameters
                }
            }
        }
    }
}