using System;

namespace StaticMocker.Fody
{
    public interface IStaticMethod
    {
        void RatherCall( Action replacement );
    }

    public interface IStaticMethod<in T>
    {
        void RatherCall( Func<T> replacement );
    }
}