using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using ApiScheme.Scheme;
using MyResources;
using ApiScheme.Client;
using System.Configuration;
using System.Threading;

namespace GameServer
{
    partial class Room
    {
        /// <summary>
        /// Saves Win/Lose info to ApiServer.
        /// </summary>
        public void SavePerks()
        {
            try
            {
                // Forms request
                var infos = new List<TransactionInfo>();
                _actors.ForEach(a =>
                {
                    if (a.character == null)
                        return;
                    var items = new CharacterItems() { };
                    if (a.Faction == FactionWon)
                    {
                        switch (a.Faction)
                        {
                            case Faction.Citizen:
                                items.WonAsCitizen = 1;
                                break;
                            case Faction.Werewolf:
                                items.WonAsWerewolf = 1;
                                break;
                            case Faction.Fox:
                                items.WonAsFox = 1;
                                break;
                        }
                    }
                    else
                    {
                        switch (a.Faction)
                        {
                            case Faction.Citizen:
                                items.LostAsCitizen = 1;
                                break;
                            case Faction.Werewolf:
                                items.LostAsWerewolf = 1;
                                break;
                            case Faction.Fox:
                                items.LostAsFox = 1;
                                break;
                        }
                    }
                    var info = new TransactionInfo() { characterName = a.character.Name, items = items };
                    infos.Add(info);
                });

                // Sends to ApiServer
                Api.Post<TransactionOut>(new TransactionIn() { infos = infos });
            }
            catch(Exception e)
            {
                Logger.WriteLine(e.ToString());
            }
        }

        public void SaveLogsAsync() {
            try {
                var t = new Thread(() => {
                    try {
                        SavePerks();
                        SaveLogs();
                    }
                    catch (Exception e){
                        Logger.WriteLine(e.ToString());
                    }
                    finally {
                        isProcessingHistory = false;
                    }
                });

                t.Priority = ThreadPriority.Lowest;
                isProcessingHistory = true;
                t.Start();
            }
            catch (Exception e) {
                Logger.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Saves logs to Azure Blob Storage.
        /// </summary>
        public void SaveLogs()
        {
            // ----- Forms HTML data -----
            var html = "";
            // CSS
            html += string.Format("<style>{0}</style>", _.PlayLogCSS);
            // Title
            html += "<a href=\"http://人狼ゲーム.com/\"><h1>人狼ゲームオンライン2</h1></a>";
            // Conf
            html += conf.ToHtml();
            // Actors
            var aliveActorsHtml = "";
            _actors.Where(a=>!a.IsDead).ToList().ForEach(a =>
            {
                aliveActorsHtml += a.ToHtml(conf.culture) + "<br/>";
            });
            html += string.Format("<div>{0}:<br/>{1}</div>", _UiString.Alive, aliveActorsHtml);
            var deadActorsHtml = "";
            _actors.Where(a => a.IsDead).ToList().ForEach(a =>
            {
                deadActorsHtml += a.ToHtml(conf.culture) + "<br/>";
            });
            html += string.Format("<div>{0}:<br/>{1}</div>", _UiString.Dead, deadActorsHtml);
            // Messages
            var messagesHtml = "";
            _messages.ForEach(m =>
            {
                messagesHtml += m.ToHtml(conf.culture);
            });
            html += string.Format("<ul class=\"messages\">{0}</ul>", messagesHtml);

            // Wraps html tag
            html = string.Format("<!DOCTYPE html><html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/><meta charset=\"utf-8\" /></head><body>{0}</body></html>", html);

            // ----- Uploads to Blob -----
            // Gets the container
            var storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("playlog");
            container.CreateIfNotExists();

            // Names
            var filename = string.Format("{0}.html", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss_fffffff"));
            var blockBlob = container.GetBlockBlobReference(filename);

            // Creates or overwrites
            var bytes = Encoding.UTF8.GetBytes(html);
            blockBlob.UploadFromByteArray(bytes, 0, bytes.Length);

            // Notifies ApiServer
            var o = ApiScheme.Client.Api.Get<AddPlayLogOut>(new AddPlayLogIn() { log = new PlayLogInfo() { culture = conf.culture.ToString(), timezone = conf.TimeZone.Id, roomName = conf.name, fileName = filename } });

            // Notifies Players
            SystemMessageAll(new []{
                new InterText("LogCanBeDownloaded", _.ResourceManager),
                new InterText(string.Format(ConfigurationManager.AppSettings["PlayLogDownloadUrl"], conf.culture.ToString(), o.id), null)
            });
            //SystemMessageAll("Uri: " + blockBlob.Uri);
            //SystemMessageAll("StorageUri: " + blockBlob.StorageUri);
            //SystemMessageAll(html);
        }
    }
}
