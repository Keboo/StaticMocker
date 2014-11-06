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
        [ExpectedException( typeof( ExpectedException ) )]
        public void WhenStaticVoidMethodIsNotMockedItCallsOriginalMethod()
        {
            var sut = new Sut();
            sut.CallsThorwingStaticVoidMethod();
        }

        [TestMethod]
        [ExpectedException( typeof( StaticMockVerificationException ) )]
        public void WhenStaticVoidMethodIsNotCalled_ItThrowsVerificationException()
        {
            using ( var staticMock = StaticMock.Create() )
            {
                var sut = new Sut();

                sut.DoesNothing();

                staticMock.Verify( () => StaticClass.VoidMethod() );
            }
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
        public void WhenStaticMethodWithReturnIsNotMocked_ItCallsOriginalMethod()
        {
            var sut = new Sut();
            sut.CallsThrowingStaticMethodWithReturnValue();
        }

        [TestMethod]
        [ExpectedException( typeof( StaticMockVerificationException ) )]
        public void WhenStaticMethodWithReturnIsNotCalled_ItThrowsVerificationException()
        {
            using ( var staticMock = StaticMock.Create() )
            {
                var sut = new Sut();

                sut.DoesNothing();

                staticMock.Verify( () => StaticClass.StringMethod() );
            }
        }


        [TestMethod]
        public void CanVerifyCallToStaticVoidMethodWithMatchingStringParameter()
        {
            using ( var staticMock = StaticMock.Create() )
            {
                var sut = new Sut();

                sut.CallsVoidWithStringParameter();

                staticMock.Verify( () => StaticClass.VoidWithStringParameter( "Some parameter" ) );
            }
        }

        [TestMethod]
        public void CanVerifyCallToStaticVoidMethodWithAnyStringParameter()
        {
            using ( var staticMock = StaticMock.Create() )
            {
                var sut = new Sut();

                sut.CallsVoidWithStringParameter();

                staticMock.Verify( () => StaticClass.VoidWithStringParameter( Param<string>.Any ) );
            }
        }

        [TestMethod]
        [ExpectedException( typeof( StaticMockVerificationException ) )]
        public void WhenCallToStaticVoidMethodWithStringDoesNotMatchParameter_IsFailsToVerify()
        {
            using ( var staticMock = StaticMock.Create() )
            {
                var sut = new Sut();

                sut.CallsVoidWithStringParameter();

                staticMock.Verify( () => StaticClass.VoidWithStringParameter( "Some other string" ) );
            }
        }


        [TestMethod]
        public void CanMockMethodWithOutParameter()
        {
            using ( var staticMocker = StaticMock.Create() )
            {
                int i;
                staticMocker.Expect( () => int.TryParse( "fubar", out i ) ).RatherCall( () => true ).UseOutValue( 2 );
                var sut = new Sut();

                int intValue;
                var result = sut.TryParseInt( "fubar", out intValue );

                Assert.IsTrue( result );
                Assert.AreEqual( 2, intValue );
            }
        }
    }
}