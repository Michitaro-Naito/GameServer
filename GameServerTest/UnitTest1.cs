using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyResources;
using System.Collections.Generic;
using ApiScheme.Client;
using ApiScheme.Scheme;
using GameServer;


namespace GameServerTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void LocalizedString()
        {
            var en = new System.Globalization.CultureInfo("en-US");
            var ja = new System.Globalization.CultureInfo("ja-JP");
            Assert.AreEqual("Hello!", _.ResourceManager.GetString("String1", en));
            Assert.AreEqual("こんにちは！", _.ResourceManager.GetString("String1", ja));
        }

        [TestMethod]
        public void ApiCall()
        {
            var str = ";klnkvfoo/foobar";
            var o = Api.Get<PlusOut>(new PlusIn() { a = 2, b = 3, echo = str });
            Assert.AreEqual(5, o.c);
            Assert.AreEqual(str, o.echo);
        }

        [TestMethod]
        public void Equals()
        {
            var foo1 = new Player() { userId = "foo", connectionId = "123" };
            var foo2 = new Player() { userId = "foo", connectionId = "456" };
            var bar = new Player() { userId = "bar", connectionId = "789" };
            Assert.IsTrue(foo1 == foo2);
            Assert.IsFalse(foo1 == bar);
            Assert.AreEqual(foo1, foo2);
            Assert.AreNotEqual(foo1, bar);
        }
    }
}
