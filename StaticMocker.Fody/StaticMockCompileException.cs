using System;

namespace StaticMocker.Fody
{
    public class StaticMockCompileException : Exception
    {
        internal StaticMockCompileException(string message)
            :base (message)
        { }
    }
}