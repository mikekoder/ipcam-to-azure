using Atlas;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Vlc.DotNet.Core;

namespace IpCamToAzure
{
    public class MyService : IAmAHostedProcess
    {
        private int duration;
        private Timer startTimer;

        public MyService()
        {
            duration = int.Parse(ConfigurationManager.AppSettings["Duration"]);
            var source = ConfigurationManager.AppSettings["Source"];
            var directory = ConfigurationManager.AppSettings["VideoDirectory"];
            var pattern = ConfigurationManager.AppSettings["NamePattern"];
            Task.Factory.StartNew(() =>
            {
                Record(source, directory, pattern);
            });

            startTimer = new Timer(duration * 1000);
            startTimer.AutoReset = true;
            startTimer.Elapsed += (sender, e) =>
            {
                Task.Factory.StartNew(() => 
                {
                    Record(source, directory, pattern);
                });
            };
        }

        private void Record(string source, string directory, string pattern)
        {
            var filename = Path.Combine(directory, string.Format(pattern, DateTime.Now.ToString("yyyyMMddHHmmss")));

            var opts = $":sout=#std{{access=file,dst='{filename}'}}";
            var player = new VlcMediaPlayer(new DirectoryInfo(ConfigurationManager.AppSettings["VlcDirectory"]), new[] { "--rtsp-tcp" });
            player.SetMedia(source, opts);
            player.EncounteredError += (sender, eventArgs) =>
            {
                player.Dispose();
                Upload(filename);
            };
            player.Play();
            var recordTimer = new Timer((duration + 2) * 1000);
            recordTimer.AutoReset = false;
            recordTimer.Elapsed += (sender, eventArgs) =>
            {
                player.Stop();
                player.Dispose();
                recordTimer.Dispose();
                Console.WriteLine($"recorded {filename}");
                Upload(filename);
            };
            Console.WriteLine($"start recording to {filename}");
            recordTimer.Start();
        }


        private void Upload(string filename)
        {
            Console.WriteLine($"start uploading {filename}");
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(ConfigurationManager.AppSettings["ContainerName"]);
            container.CreateIfNotExists();

            var blockBlob = container.GetBlockBlobReference(new FileInfo(filename).Name);

            using (var fileStream = System.IO.File.OpenRead(filename))
            {
                blockBlob.UploadFromStream(fileStream);
            }

            Console.WriteLine($"uploaded {filename}");
            File.Delete(filename);
        }
        public void Start()
        {
            startTimer.Start();
        }

        public void Stop()
        {
            startTimer.Stop();
            startTimer.Dispose();
        }

        public void Pause()
        {
            startTimer.Stop();
        }

        public void Resume()
        {
            startTimer.Start();
        }

    }
}
