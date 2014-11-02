using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;

namespace StaticMocker.Fody.Tests
{
    [TestClass]
    public class Global
    {
        [AssemblyInitialize]
        public static void Setup( TestContext context )
        {
            //StaticMockWeaver.InterceptStatics( @".\LibraryToTest.dll", new DefaultAssemblyResolver() );
        }

        [TestMethod]
        public void Foo()
        {
            Assert.Fail();
        }
    }
}