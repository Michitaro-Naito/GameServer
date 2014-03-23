using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameServer;
using System.Collections.Generic;

namespace GameServerTest
{
    [TestClass]
    public class ActorTest
    {
        /*[TestMethod]
        public void RandomName()
        {
            var actors = new List<Actor>();
            for (var n = 0; n < 10000; n++)
                actors.Add(new Actor());
            Assert.IsFalse(actors.All(a => "Pioneer" == a.title.GetString(new System.Globalization.CultureInfo("en-US"))));
            Assert.IsFalse(actors.All(a => "Alice" == a.name.GetString(new System.Globalization.CultureInfo("en-US"))));
        }*/

        [TestMethod]
        public void UniqueTitleAndName()
        {
            var actors = Actor.Create(32);
            actors.ForEach(actor =>
            {
                Assert.IsFalse(actors.Any(a => a != actor && a.title.ToString() == actor.title.ToString()));
                Assert.IsFalse(actors.Any(a => a != actor && a.name.ToString() == actor.name.ToString()));
            });
        }

        [TestMethod]
        public void UniqueTitleAndNameNotEnoughKeys()
        {
            try
            {
                var actors = Actor.Create(1000000);
            }
            catch (ArgumentException)
            {
                return;
            }
            Assert.Fail("ArgumentException not thrown");
        }
    }
}
