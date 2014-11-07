using System;

namespace StaticMocker.Fody
{
    public interface IStaticMethod
    {
        IStaticMethod RatherCall( Action replacement );
        IStaticMethod UseOutValue<TOut>( TOut outValue, string parameterName = null );
    }

    public interface IStaticMethod<in T>
    {
        IStaticMethod<T> RatherCall( Func<T> replacement );
        IStaticMethod<T> UseOutValue<TOut>( TOut outValue, string parameterName = null );
    }
}