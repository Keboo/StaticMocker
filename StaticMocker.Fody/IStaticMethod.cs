using System;

namespace StaticMocker.Fody
{
    public interface IStaticMethod
    {
        IStaticMethod RatherCall( Action replacement );
        void UseOutValue<TOut>( TOut outValue, string parameterName = null );
    }

    public interface IStaticMethod<in T>
    {
        IStaticMethod<T> RatherCall( Func<T> replacement );
        void UseOutValue<TOut>( TOut outValue, string parameterName = null );
    }
}