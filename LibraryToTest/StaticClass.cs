using System;

namespace LibraryToTest
{
    public static class StaticClass
    {
        public static void VoidMethod()
        {

        }

        public static void ThrowingVoidMethod()
        {
            throw new ExpectedException();
        }

        public static string StringMethod()
        {
            return "This is a NOT a testing string";
        }

        public static string ThrowingStringMethod()
        {
            throw new ExpectedException();
        }
    }

    public class ExpectedException : Exception
    {
        public ExpectedException()
            : base( "This exception is expected to be thrown" )
        { }
    }
}