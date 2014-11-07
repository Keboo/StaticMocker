using System;

namespace StaticMocker.Fody
{
    public class StaticMockException : Exception
    {
        internal StaticMockException( string message )
            : base( message )
        { }
    }
}