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
            var charactersToRemove = _characters.Where(c => c.Player != null && c.Player.userId == userId).ToList();
            charactersToRemove.ForEach(c =>
            {
                c.Room = null;
                _characters.Remove(c);
            });
            //var amountKicked = _characters.RemoveAll(c => c.Player != null && c.Player.userId == userId);
            var amountKicked = charactersToRemove.Count;

            // Removes Actors?
            if (!new[] { RoomState.Configuring, RoomState.Matchmaking, RoomState.Playing }.Contains(RoomState))
                // Don't have to.
                return amountKicked;
            // Kicks
            _actors.Where(a => a.character != null
                && a.character.Player.userId == userId    // Owned by Player.
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
