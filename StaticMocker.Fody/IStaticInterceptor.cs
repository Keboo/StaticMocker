
namespace StaticMocker.Fody
{
    internal interface IStaticInterceptor
    {
        bool AllowMethodCall( MockMethod mockMethod );
    }
}