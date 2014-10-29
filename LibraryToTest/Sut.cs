namespace LibraryToTest
{
    public class Sut
    {
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
    }
}