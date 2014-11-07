using System;

namespace StaticMocker.Fody
{
    public class StaticMockVerificationException : StaticMockException
    {
        internal StaticMockVerificationException(string message)
            :base(message)
        { }
    }
}