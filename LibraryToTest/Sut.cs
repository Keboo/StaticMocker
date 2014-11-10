using System;

namespace LibraryToTest
{
    public class Sut
    {
        public void DoesNothing()
        { }

        public void CallsStaticVoidMethod()
        {
            StaticClass.VoidMethod();
        }

        public void CallsThorwingStaticVoidMethod()
        {
            StaticClass.ThrowingVoidMethod();
        }

        public string CallsStaticMethodWithReturnValue()
        {
            return StaticClass.StringMethod();
        }

        public string CallsThrowingStaticMethodWithReturnValue()
        {
            return StaticClass.ThrowingStringMethod();
        }

        public void CallsVoidWithStringParameter()
        {
            string parameter = "Some parameter";
            StaticClass.VoidWithStringParameter( parameter );
        }

        public bool TryParseInt( string @string, out int intValue )
        {
            return int.TryParse( @string, out intValue );
        }

        public string CallsMultipleOutParameters( out int first, out int second )
        {
            string rv;
            StaticClass.MultipleOutParameters( out first, out rv, out second );
            return rv;
        }

        public Tuple<Guid, int> MakesMultipleStaticCalls( string toParse )
        {
            return new Tuple<Guid, int>( Guid.NewGuid(), int.Parse( toParse ) );
        }

        public int CallsIntParse( string inputString )
        {
            return int.Parse( inputString );
        }
    }
}