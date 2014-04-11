using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    partial class Room
    {
        internal void Kick(string userId)
        {
            _characters.RemoveAll(c => c.Player!=null && c.Player.userId == userId);
            _actors.Where(a => a.character!=null && a.character.Player.userId==userId).ToList().ForEach(a =>
            {
                // Notifies players that someone gone.
                SystemMessageAll(new InterText("AHasGoneFromB", MyResources._.ResourceManager, new[] { new InterText(a.character.Name, null), a.TitleAndName }));

                // Removes
                a.character = null;

                _needSync = true;
            });
        }
    }
}
