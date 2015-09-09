using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VS.ConfigurationManager;

namespace UninstallerTests
{
    [TestClass]
    public class ConfigurationManagerTests
    {
        [TestMethod]
        public void BundleConstructorTest()
        {
            Bundle bundle = new Bundle();
            Assert.IsTrue(bundle.BundleId == Guid.Empty);
        }
    }
}
