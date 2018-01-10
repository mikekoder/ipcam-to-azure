using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Vlc.DotNet.Core;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Atlas;

namespace IpCamToAzure
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceName = ConfigurationManager.AppSettings["ServiceName"];
            var configuration = Host.Configure<MyService>().WithArguments(args).Named(serviceName);
            Host.Start(configuration);
        }
    }
}
