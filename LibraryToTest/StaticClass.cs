﻿namespace LibraryToTest
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

        public static void VoidWithStringParameter(string strParameter)
        {
            
        }

        public static void MultipleOutParameters( out int one, out string emptyString, out int two )
        {
            one = 1;
            two = 2;
            emptyString = "";
        }
    }
}