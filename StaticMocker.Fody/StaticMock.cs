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
            private readonly Dictionary<MockMethod, StaticMethod> _ExpectedMethodCalls = new Dictionary<MockMethod, StaticMethod>();

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
                var method = GetMockMethod( methodExpression );
                var rv = new StaticVoidMethod();
                _ExpectedMethodCalls[method] = rv;
                return rv;
            }

            public IStaticMethod<T> Expect<T>( Expression<Func<T>> methodExpression )
            {
                var method = GetMockMethod( methodExpression );
                var rv = new StaticReturnMethod<T>();
                _ExpectedMethodCalls[method] = rv;
                return rv;
            }

            bool IStaticInterceptor.AllowMethodCall( MockMethod mockMethod )
            {
                _CalledMethods.Add( mockMethod );
                StaticMethod staticMethod;
                if ( _ExpectedMethodCalls.TryGetValue( mockMethod, out staticMethod ) )
                {
                    staticMethod.Handle( mockMethod );
                    return false;
                }
                return true;
            }

            private static MockMethod GetMockMethod( Expression expression )
            {
                var methodExpression = GetMethodExpression( expression );
                var expressionArguments = GetMethodExpressionArguments( methodExpression );
                var methodInfo = methodExpression.Method;
                IList<Param> parameters = new List<Param>();

                var expressionArgumentIndex = 0;
                foreach ( ParameterInfo parameter in methodInfo.GetParameters() )
                {
                    if ( parameter.IsOut )
                    {
                        parameters.Add( Param.Out( parameter.Name, parameter.ParameterType.GetElementType() ) );
                    }
                    else
                    {
                        parameters.Add( Param.In( parameter.Name, parameter.ParameterType,
                            expressionArguments[expressionArgumentIndex++] ) );
                    }
                }
                return new MockMethod( methodInfo, parameters );
            }

            private static IList<object> GetMethodExpressionArguments( MethodCallExpression methodExpression )
            {
                var rv = new List<object>();
                foreach ( var argumentExpression in methodExpression.Arguments )
                {
                    switch ( argumentExpression.NodeType )
                    {
                        case ExpressionType.Constant:
                            rv.Add( ( (ConstantExpression)argumentExpression ).Value );
                            break;
                        case ExpressionType.MemberAccess:
                            //This will be the out parameter
                            break;
                        default:
                            //TODO: Better exception type
                            throw new Exception( string.Format( "Could not get value from {0} expression", argumentExpression.NodeType ) );
                    }
                }
                return rv;
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
                private readonly Dictionary<Tuple<Type, string>, object> _OutParameterValues =
                    new Dictionary<Tuple<Type, string>, object>();
                public abstract void Handle( MockMethod mockMethod );

                protected object GetOutValue( Type parameterType, string parameterName )
                {
                    object rv;
                    if ( _OutParameterValues.TryGetValue( Tuple.Create( parameterType, parameterName ), out rv ) )
                    {
                        return rv;
                    }
                    if ( _OutParameterValues.TryGetValue( Tuple.Create( parameterType, (string)null ), out rv ) )
                    {
                        return rv;
                    }
                    //TODO: Better exception type
                    throw new Exception( string.Format( "Could not find out parameter {0} {1}", parameterType.FullName, parameterName ) );
                }

                public void UseOutValue<TOut>( TOut outValue, string parameterName = null )
                {
                    _OutParameterValues[Tuple.Create( typeof( TOut ), parameterName )] = outValue;
                }
            }

            private class StaticVoidMethod : StaticMethod, IStaticMethod
            {
                private Action _ReplacementCall;

                public IStaticMethod RatherCall( Action replacement )
                {
                    if ( replacement == null ) throw new ArgumentNullException( "replacement" );
                    _ReplacementCall = replacement;
                    return this;
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

                public IStaticMethod<T> RatherCall( Func<T> replacement )
                {
                    if ( replacement == null ) throw new ArgumentNullException( "replacement" );
                    _ReplacementCall = replacement;
                    return this;
                }

                public override void Handle( MockMethod mockMethod )
                {
                    if ( _ReplacementCall != null )
                    {
                        mockMethod.ReturnValue = _ReplacementCall();
                    }
                    foreach ( var outParam in mockMethod.Parameters.Where( x => x.ParameterType == ParameterType.Out ) )
                    {
                        outParam.Value = GetOutValue( outParam.Type, outParam.Name );
                    }
                }
            }
        }
    }
}