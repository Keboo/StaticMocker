using System;
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
                staticMock.Expect( () => StaticClass.ThrowingVoidMethod() ).Return( () =>
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
                staticMock.Expect( () => StaticClass.ThrowingStringMethod() ).Return( () => expected );
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
                staticMocker.Expect( () => int.TryParse( "fubar", out i ) ).Return( () => true ).UseOutValue( 2 );
                var sut = new Sut();

                int intValue;
                var result = sut.TryParseInt( "fubar", out intValue );

                Assert.IsTrue( result );
                Assert.AreEqual( 2, intValue );
            }
        }

        [TestMethod]
        public void WhenMockingMethodWithMultipleOutParameters_ItUsesParametersByName()
        {
            using ( var staticMocker = StaticMock.Create() )
            {
                staticMocker.Expect( () => StaticClass.MultipleOutParameters( out Param<int>.Any, out Param<string>.Any, out Param<int>.Any ) )
                    .UseOutValue( 42, "one" ).UseOutValue( 43, "two" ).UseOutValue( "my string value", "emptyString" );
                var sut = new Sut();

                int first, second;
                var result = sut.CallsMultipleOutParameters( out first, out second );

                Assert.AreEqual( "my string value", result );
                Assert.AreEqual( 42, first );
                Assert.AreEqual( 43, second );
            }
        }

        [TestMethod]
        public void CanMockMultipleStaticCalls()
        {
            using ( var staticMocker = StaticMock.Create() )
            {
                var myGuid = new Guid( "979EE592-5F5F-456A-A5A0-79D6696FFB5B" );
                staticMocker.Expect( () => Guid.NewGuid() ).Return( () => myGuid );
                staticMocker.Expect( () => int.Parse( Param<string>.Any ) ).Return( () => 5 );
                var sut = new Sut();

                var result = sut.MakesMultipleStaticCalls( "fubar" );

                Assert.AreEqual( Tuple.Create( myGuid, 5 ), result );
            }
        }
    }
}