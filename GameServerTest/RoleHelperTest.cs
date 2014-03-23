using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameServer;

namespace GameServerTest
{
    [TestClass]
    public class RoleHelperTest
    {
        [TestMethod]
        public void CastRolesAuto()
        {
            for (var n = 7; n <= 64; n++)
            {
                for (var m = 0; m < 100; m++)
                {
                    var dic = RoleHelper.CastRolesAuto(n);

                    // Total Casted == Total Requested
                    Assert.AreEqual(n, dic.Sum(p => p.Value));

                    // There is no Role.None
                    Assert.AreEqual(0, dic[Role.None]);

                    // Count must be >= 0
                    Assert.IsTrue(dic.All(p => p.Value >= 0));
                }
            }
        }
    }
}
