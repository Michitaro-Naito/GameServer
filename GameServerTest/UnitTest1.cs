using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyResources;
using System.Collections.Generic;
using ApiScheme.Client;
using ApiScheme.Scheme;
using GameServer;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;


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

        [TestMethod]
        public void Concurrent()
        {
            var q = new ConcurrentQueue<int>();

            // Populates the queue.
            for (int i = 0; i < 10000; i++)
                q.Enqueue(i);

            // Peek at the first element.
            int result;
            if (!q.TryPeek(out result))
            {
                Assert.Fail("CQ: TryPeek failed when it should have succeeded");
            }
            else if (result != 0)
            {
                Assert.AreEqual(0, result);
            }

            int outerSum = 0;
            // An action to consume the ConcurrentQueue.
            Action action = () =>
            {
                int localSum = 0;
                int localValue;
                while (q.TryDequeue(out localValue))
                    localSum += localValue;
                Interlocked.Add(ref outerSum, localSum);
            };

            // Start 4 concurrent consuming actions.
            Parallel.Invoke(action, action, action, action);

            Assert.AreEqual(49995000, outerSum);
        }

        public enum Foo
        {
            Bar
        }
        [TestMethod]
        public void ToLocalizedString()
        {
            var en = new CultureInfo("en-US");
            var ja = new CultureInfo("ja-JP");
            Assert.AreEqual("Foo_Bar", Foo.Bar.ToKey());
            Assert.AreEqual(_Enum.ResourceManager.GetString("Foo_Bar", en), Foo.Bar.ToLocalizedString(en));
            Assert.AreEqual(_Enum.ResourceManager.GetString("Foo_Bar", ja), Foo.Bar.ToLocalizedString(ja));
        }

        [TestMethod]
        public void ColorCode()
        {
            for (var n = 0; n < 100; n++)
            {
                var name = UT.RandomString(10);
                var colorIdentity = ColorHelper.GenerateColorIdentity(name);
                Console.WriteLine(colorIdentity);
            }
        }
    }
}
