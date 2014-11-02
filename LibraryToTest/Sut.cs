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
    }
}