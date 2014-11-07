using System;

namespace StaticMocker.Fody
{
    public interface IStaticMethod
    {
        IStaticMethod Return( Action replacement );
        IStaticMethod UseOutValue<TOut>( TOut outValue, string parameterName = null );
    }

    public interface IStaticMethod<in T>
    {
        IStaticMethod<T> Return( Func<T> replacement );
        IStaticMethod<T> UseOutValue<TOut>( TOut outValue, string parameterName = null );
    }
}