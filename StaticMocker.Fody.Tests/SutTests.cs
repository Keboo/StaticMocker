using LibraryToTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StaticMocker.Fody.Tests
{
    [TestClass]
    public class SutTests
    {
        [TestMethod]
        public void CanVerifyCallToStaticVoidMethod()
        {
            using ( var staticMock = StaticMock.Create() )
            {
                var sut = new Sut();
        
                sut.CallsStaticVoidMethod();
        
                staticMock.Verify( () => StaticClass.VoidMethod() );
            }
        }
        
        [TestMethod]
        public void CanReplaceCallToStaticVoidMethod()
        {
            using ( var staticMock = StaticMock.Create() )
            {
                bool wasCalled = false;
                staticMock.Expect( () => StaticClass.ThrowingVoidMethod() ).RatherCall( () =>
                {
                    wasCalled = true;
                } );
                var sut = new Sut();
        
                sut.CallsThorwingStaticVoidMethod();
        
                staticMock.Verify( () => StaticClass.ThrowingVoidMethod() );
                Assert.IsTrue( wasCalled );
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ExpectedException))]
        public void WhenStaticVoidMethodIsNotMockedItCallsOriginalMethod()
        {
            var sut = new Sut();
            sut.CallsThorwingStaticVoidMethod();
        }

        [TestMethod]
        public void CanVerifyCallToStaticMethodWithReturn()
        {
            using ( var staticMock = StaticMock.Create() )
            {
                var sut = new Sut();
        
                sut.CallsStaticMethodWithReturnValue();
        
                staticMock.Verify( () => StaticClass.StringMethod() );
            }
        }
        
        [TestMethod]
        public void CanReplaceCallToStaticMethodWithReturn()
        {
            const string expected = "expected string";
            using ( var staticMock = StaticMock.Create() )
            {
                staticMock.Expect( () => StaticClass.ThrowingStringMethod() ).RatherCall( () => expected );
                var sut = new Sut();
        
                var actual = sut.CallsThrowingStaticMethodWithReturnValue();
        
                Assert.AreEqual( expected, actual );
                staticMock.Verify( () => StaticClass.ThrowingStringMethod() );
            }
        }

        [TestMethod]
        [ExpectedException( typeof( ExpectedException ) )]
        public void WhenStaticMethodWithReturnIsNotMockedItCallsOriginalMethod()
        {
            var sut = new Sut();
            sut.CallsThrowingStaticMethodWithReturnValue();
        }
    }
}