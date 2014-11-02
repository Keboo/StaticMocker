using System;

namespace LibraryToTest
{
    public class ExpectedException : Exception
    {
        public ExpectedException()
            : base( "This exception is expected to be thrown" )
        { }
    }
}