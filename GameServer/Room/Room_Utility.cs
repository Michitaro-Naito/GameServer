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
        internal void Kick(string userId)
        {
            // Removes from connected characters.
            var charactersToRemove = _characters.Where(c => c.Player != null && c.Player.userId == userId).ToList();
            charactersToRemove.ForEach(c =>
            {
                _actors.Where(a => a.character == c).ToList().ForEach(a => a.lastAccess = DateTime.UtcNow);
                c.Room = null;
                if(c.Player != null)
                    c.Player.BroughtTo(ClientState.Rooms);
                _characters.Remove(c);
            });
            //var amountKicked = charactersToRemove.Count;

            // Removes Spectators
            var spectatorsToRemove = _spectators.Where(s => s.Player != null && s.Player.userId == userId).ToList();
            spectatorsToRemove.ForEach(s => {
                if (s.Player != null)
                    s.Player.BroughtTo(ClientState.Rooms);
                _spectators.Remove(s);
            });
            //amountKicked += spectatorsToRemove.Count;
            /*_spectators.Where(s => s.Player != null && s.Player.userId == userId).ToList().ForEach(s => {
                if (s.Player != null)
                    s.Player.BroughtTo(ClientState.Rooms);
            });
            amountKicked += _spectators.RemoveAll(s => s.Player != null && s.Player.userId == userId);*/

            if(charactersToRemove.Count > 0)
                // Sync only when Character kicked.
                _needSync = true;
        }

        void QueueSyncForCharacter(Character c) {
            if (!charactersNeedSync.Contains(c))
                charactersNeedSync.Add(c);
        }

        internal void SendRules()
        {
            /*村の掟
--------------------
・ささやきを使用できます。(恋人だけは受信したささやきを見れません。)
・初日占いがあります。(占い師はランダムにひとり、村人チームのメンバーがわかった状態でスタートします。)
・霊媒師が弱体化されています。(霊媒師は犠牲者の種族(村人,人狼,妖狐)しかわかりません。)
・COボタンがあります。(システムメッセージを使って自らの役職を宣言できます。(使用推奨))
--------------------*/
            var messages = new List<InterText>();
            messages.Add(new InterText("村の掟", null));
            messages.Add(new InterText("--------------------", null));
            messages.Add(new InterText("・ささやきを使用できます。", null));
            messages.Add(new InterText("・初日占いがあります。(占い師はランダムにひとり、村人チームのメンバーがわかった状態でスタートします。)", null));
            messages.Add(new InterText("・霊媒師が弱体化されています。(霊媒師は犠牲者の種族(村人,人狼,妖狐)しかわかりません。)", null));
            messages.Add(new InterText("--------------------", null));
            SystemMessageAll(messages.ToArray());
        }

        internal RoomInfo ToInfo()
        {
            return new RoomInfo()
            {
                roomId = roomId,
                guid = guid,
                name = conf.name,
                max = conf.max,
                interval = conf.interval,
                requiresPassword = RequiresPassword,
                alivePlayers = AliveActors.Count(a=>!a.IsNPC),
                aliveActors = AliveActors.Count(),
                state = RoomState
            };
        }
    }
}
