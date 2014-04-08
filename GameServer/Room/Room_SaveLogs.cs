using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

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

            // Forms HTML data
            var html = "";
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
            var filename = string.Format("{0}.html", DateTime.UtcNow.ToString("yyyy-MM-dd_hh-mm-ss_fffffff"));
            var blockBlob = container.GetBlockBlobReference(filename);

            // Creates or overwrites
            var bytes = Encoding.UTF8.GetBytes(html);
            blockBlob.UploadFromByteArray(bytes, 0, bytes.Length);

            // Notifies Players
            SystemMessageAll("Uri: " + blockBlob.Uri);
            SystemMessageAll("StorageUri: " + blockBlob.StorageUri);
            SystemMessageAll(html);
        }
    }
}
