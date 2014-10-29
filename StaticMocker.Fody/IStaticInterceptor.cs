using System.Reflection;

namespace StaticMocker.Fody
{
    internal interface IStaticInterceptor
    {
        bool AllowMethodCall(MethodInfo methodInfo);
        bool AllowMethodCall(MethodInfo methodInfo, out object replacement);
    }
}