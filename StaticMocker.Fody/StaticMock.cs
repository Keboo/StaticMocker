using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
            private readonly List<MockMethod> _CalledMethods = new List<MockMethod>();
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
                var mockMethod = GetMockMethod( methodExpression );

                if ( !_CalledMethods.Any( mockMethod.IsMatch ) )
                {
                    throw new StaticMockVerificationException( string.Format( "{0} was not invoked", mockMethod ) );
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
                var key = _ExpectedMethodCalls.Keys.FirstOrDefault(x => x.IsMatch(mockMethod));
                if ( key != null )
                {
                    _ExpectedMethodCalls[key].Handle( mockMethod );
                    return false;
                }
                return true;
            }

            private static MockMethod GetMockMethod( Expression expression )
            {
                var methodExpression = GetMethodExpression( expression );
                var methodInfo = methodExpression.Method;
                var methodParameters = GetMethodParameters( methodExpression );

                return new MockMethod( methodInfo, methodParameters );
            }

            private static IList<Param> GetMethodParameters( MethodCallExpression methodExpression )
            {
                var methodInfo = methodExpression.Method;
                var parameterInfos = methodInfo.GetParameters();

                IList<Param> parameters = new List<Param>();

                for ( int i = 0; i < parameterInfos.Length; i++ )
                {
                    var parameter = parameterInfos[i];
                    var argumentExpression = methodExpression.Arguments[i];

                    Type parameterType = parameter.IsOut
                        ? parameter.ParameterType.GetElementType()
                        : parameter.ParameterType;
                    string parameterName = parameter.Name;
                    ParameterType paramType = parameter.IsOut ? ParameterType.Out : ParameterType.In;

                    switch ( argumentExpression.NodeType )
                    {
                        case ExpressionType.Constant:

                            if ( parameter.IsOut )
                            {
                                parameters.Add( Param.Out( parameterName, parameterType ) );
                            }
                            else
                            {
                                parameters.Add( Param.In( parameterName, parameterType,
                                    ( (ConstantExpression)argumentExpression ).Value ) );
                            }
                            break;
                        case ExpressionType.MemberAccess:
                            if ( IsAnyParam( (MemberExpression)argumentExpression ) )
                            {
                                parameters.Add( Param.CreateAny( parameterName, parameterType, paramType ) );
                            }
                            else if ( parameter.IsOut )
                            {
                                parameters.Add( Param.Out( parameterName, parameterType ) );
                            }
                            //Else: This will be the out parameter case
                            break;
                        default:
                            //TODO: Better exception type
                            throw new Exception( string.Format( "Could not get value from {0} expression", argumentExpression.NodeType ) );
                    }
                }
                return parameters;
            }

            private static bool IsAnyParam( MemberExpression argumentExpression )
            {
                return typeof( Param<> ).MakeGenericType( argumentExpression.Type ).GetField( "Any" ) == argumentExpression.Member;
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

                public virtual void Handle(MockMethod mockMethod)
                {
                    foreach ( var outParam in mockMethod.Parameters.Where( x => x.ParameterType == ParameterType.Out ) )
                    {
                        outParam.Value = GetOutValue( outParam.Type, outParam.Name );
                    }
                }

                private object GetOutValue( Type parameterType, string parameterName )
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

                protected void UseOutValueImpl<TOut>( TOut outValue, string parameterName )
                {
                    _OutParameterValues[Tuple.Create( typeof( TOut ), parameterName )] = outValue;
                }
            }

            private class StaticVoidMethod : StaticMethod, IStaticMethod
            {
                private Action _ReplacementCall;

                public IStaticMethod Return( Action replacement )
                {
                    if ( replacement == null ) throw new ArgumentNullException( "replacement" );
                    _ReplacementCall = replacement;
                    return this;
                }

                public IStaticMethod UseOutValue<TOut>( TOut outValue, string parameterName = null )
                {
                    UseOutValueImpl( outValue, parameterName );
                    return this;
                }

                public override void Handle( MockMethod mockMethod )
                {
                    if ( _ReplacementCall != null )
                        _ReplacementCall();
                    base.Handle(mockMethod);
                }
            }

            private class StaticReturnMethod<T> : StaticMethod, IStaticMethod<T>
            {
                private Func<T> _ReplacementCall;

                public IStaticMethod<T> Return( Func<T> replacement )
                {
                    if ( replacement == null ) throw new ArgumentNullException( "replacement" );
                    _ReplacementCall = replacement;
                    return this;
                }

                public IStaticMethod<T> UseOutValue<TOut>(TOut outValue, string parameterName = null)
                {
                    UseOutValueImpl(outValue, parameterName);
                    return this;
                }

                public override void Handle( MockMethod mockMethod )
                {
                    if ( _ReplacementCall != null )
                    {
                        mockMethod.ReturnValue = _ReplacementCall();
                    }
                    base.Handle(mockMethod);
                }
            }
        }
    }
}