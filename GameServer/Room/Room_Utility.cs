using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    partial class Room
    {
        /// <summary>
        /// Kicks Player out from this room.
        /// </summary>
        /// <param name="userId"></param>
        internal int Kick(string userId)
        {
            // Removes from connected characters.
            var amountKicked = _characters.RemoveAll(c => c.Player != null && c.Player.userId == userId);

            // Removes Actors?
            if (!new[] { RoomState.Configuring, RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState))
                // Don't have to.
                return amountKicked;
            // Kicks
            _actors.Where(a => a.character!=null
                && a.character.Player.userId==userId    // Owned by Player.
                && !a.IsDead                            // Not dead.
                ).ToList().ForEach(a =>
            {
                // Notifies players that someone gone.
                SystemMessageAll(new InterText("AHasGoneFromB", MyResources._.ResourceManager, new[] { new InterText(a.character.Name, null), a.TitleAndName }));

                // Removes
                a.character = null;

                _needSync = true;
            });

            return amountKicked;
        }
    }
}
