using System;

namespace StaticMocker.Fody
{
    public class StaticMockVerificationException : Exception
    {
        internal StaticMockVerificationException(string message)
            :base(message)
        { }
    }
}