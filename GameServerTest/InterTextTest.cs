using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyResources;
using GameServer;
using System.Globalization;

namespace GameServerTest
{
    [TestClass]
    public class InterTextTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var str = string.Format("{0},{1}", new[] { "Foo", "Bar" });
            Assert.AreEqual("Foo,Bar", str);
        }

        [TestMethod]
        public void Format()
        {
            var en = new CultureInfo("en-US");
            var ja = new CultureInfo("ja-JP");

            // Not localized
            var text = new InterText("{0} is {1}", null, new[] { new InterText("Foo", null), new InterText("Bar", null) });
            Assert.AreEqual("Foo is Bar", text.ToString());
            Assert.AreEqual("Foo is Bar", text.GetString(en));
            Assert.AreEqual("Foo is Bar", text.GetString(ja));

            // Localized
            text = new InterText("AIsB", _Test.ResourceManager, new[] { new InterText("Foo", _Test.ResourceManager), new InterText("Bar", _Test.ResourceManager) });
            Assert.AreEqual("Foo! is Bar!.", text.ToString());
            Assert.AreEqual("Foo! is Bar!.", text.GetString(en));
            Assert.AreEqual("ふーはばーです。", text.GetString(ja));
        }
    }
}
