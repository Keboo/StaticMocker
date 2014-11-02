using System;
using System.Linq.Expressions;

namespace StaticMocker.Fody
{
    public interface IStaticMock : IDisposable
    {
        void Verify( Expression<Action> methodExpression );
        void Verify<T>( Expression<Func<T>> methodExpression );
        IStaticMethod Expect( Expression<Action> methodExpression );
        IStaticMethod<T> Expect<T>( Expression<Func<T>> methodExpression );
    }
}