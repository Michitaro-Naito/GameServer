using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;
using System.Threading;
using System.Net;
using System.Configuration;

namespace GameServer
{
    class Program
    {
        // GameServer.exe [Debug/Release] [host(192.168.100.3)] [name(Tokyo#1)]
        static void Main(string[] args)
        {
            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 to bind to all addresses. 
            // See http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx 
            // for more information.
            Console.WriteLine(GameConfiguration.ToString());
            Logger.WriteLine("Starting");

            string url = GameConfiguration.ListenUrl;   //"http://*:8080";
            var t = new Thread(new ThreadStart(() =>
            {
                while(true){
                    MyHub.Update();
                    Thread.Sleep(1);
                }
            }));
            t.Start();
            using (WebApp.Start(url))
            {
                Console.WriteLine("Server running on {0}", url);
                Console.ReadLine();
            }
            t.Abort();
        }
    }
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
}