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

namespace GameServer
{
    partial class Room
    {
        /// <summary>
        /// Saves logs to Azure Blob Storage.
        /// </summary>
        public void SaveLogs()
        {
            SystemMessageAll("Saving logs...");

            // ----- Forms HTML data -----
            var html = "";
            // Conf
            html += conf.ToHtml();
            // Actors
            var aliveActorsHtml = "";
            _actors.Where(a=>!a.IsDead).ToList().ForEach(a =>
            {
                aliveActorsHtml += a.ToHtml(conf.culture);
            });
            html += string.Format("<div>{0}: {1}</div>", _UiString.Alive, aliveActorsHtml);
            var deadActorsHtml = "";
            _actors.Where(a => a.IsDead).ToList().ForEach(a =>
            {
                deadActorsHtml += a.ToHtml(conf.culture);
            });
            html += string.Format("<div>{0}: {1}</div>", _UiString.Dead, deadActorsHtml);
            // Messages
            _messages.ForEach(m =>
            {
                html += m.ToHtml(conf.culture);
            });

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
            ApiScheme.Client.Api.Get<AddPlayLogOut>(new AddPlayLogIn() { roomName = conf.name, fileName = filename });

            // Notifies Players
            SystemMessageAll("Uri: " + blockBlob.Uri);
            SystemMessageAll("StorageUri: " + blockBlob.StorageUri);
            SystemMessageAll(html);
        }
    }
}
