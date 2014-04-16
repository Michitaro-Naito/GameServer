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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure;
using System.Text.RegularExpressions;
using System.Linq;


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

        [TestMethod]
        public void Blob()
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            CloudBlobContainer container = blobClient.GetContainerReference("playlog");

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();
        }

        [TestMethod]
        public void Unicode()
        {
            /*var str = "GOOD_STR BAD鿌鿋鼀鐏　STRINGMicrosoft .NET Framework（マイクロソフト ドットネット フレームワーク）は、マイクロソフトが開発したアプリケーション開発、実行環境。"
+ "WindowsアプリケーションだけでなくXML WebサービスやウェブアプリケーションなどWebベースのアプリケーションなどを取り入れた環境。一般に.NETという場合、.NET全体の環境を指す。"
+ "また.NET Frameworkの基盤となっている仕様である共通言語基盤 (CLI) はEcmaインターナショナル、ISO、JISにて標準化されており[1][2][3]、マイクロソフト以外のベンダーが独自に実装することもできる。実際にXamarinによるMonoプロジェクトをはじめ、いくつかのオープンソースによる実装プロジェクトがある。それらを使うことで.NET FrameworkでコンパイルしたプログラムをLinuxやMac OS XなどのWindows以外のOSでも動かすこともできる。なお、CLIのマイクロソフトの実装を共通言語ランタイム (CLR) と呼ぶ。.NET FrameworkはCLRにその他ライブラリ群を加えたものと言える。"
+ "近年では共通言語ランタイム上でJava仮想マシンの実装を試みるIKVM.NETなどのオープンソースプロジェクトも活発化している。";*/
            var goodNames = new List<string>()
            {
                "ABCDEFG",
                "Foo123",
                "90Bar90",
                "日本語の_名前",
                "____GOD___",
                "カタカナ",
                "안녕하세요",
                "fooༀض"
            };
            var badNames = new List<string>()
            {
                " test  ",
                "test　",
                "foo\u0C80"
            };
            var regex = new Regex(@"^\w{1,10}$", RegexOptions.None);
            /*foreach (Match match in regex.Matches(str))
            {
                Console.WriteLine(string.Format("\"{0}\"", match.Value));
            }*/
            goodNames.ForEach(name => Assert.IsTrue(regex.IsMatch(name)));
            badNames.ForEach(name => Assert.IsFalse(regex.IsMatch(name)));
        }

        [TestMethod]
        public void NGWord()
        {
            var goodNames = new List<string>()
            {
                "善意のユーザー",
                "NiceGuy",
                "PikkkaChu",
                "よいユーザー0001０１２３"
            };
            var badNames = new List<string>()
            {
                "おちんちん",
                "ちんぽ",
                "チんコ",
                "まんまん",
                "死ね"
            };
            goodNames.ForEach(name=>Assert.IsFalse(NGWordHelper.Regex.IsMatch(name)));
            badNames.ForEach(name=>Assert.IsTrue(NGWordHelper.Regex.IsMatch(name)));
        }

        [TestMethod]
        public void Enqueue()
        {
            var amount = 100000000;
            var queue = new ConcurrentQueue<int>();
            Parallel.For(0, amount, n => queue.Enqueue(n));
            Assert.AreEqual(queue.Count, amount);
        }
    }
}
