using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Server;
namespace r2warsTorneo
{
    static public class r2warsStatic
    {
        static public r2wars r2w = new r2wars();
        static public Torneo torneo = new Torneo();
    }
    class Program
    {
        static void Main(string[] args)
        {
            string httpUrl = "http://127.0.0.1:9664";
            string websocketUrl = "ws://127.0.0.1:9966";
            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;
            if (args.Length > 0) {
              r2warsStatic.torneo.SetWarriorsDirectory(args[0]);
            }

            var taskA = new Task(() =>
            {
                var nancyHost = new Nancy.Hosting.Self.NancyHost(new Uri(httpUrl), new CustomBootstrapper());
                nancyHost.Start();
                Console.WriteLine("Web server running at " + httpUrl);

                Process.Start(httpUrl);


                while (!ct.IsCancellationRequested) { Thread.Sleep(1000); }
                nancyHost.Stop();
            }, tokenSource.Token);

            var taskB = new Task(() =>
            {
                var wssv = new WebSocketServer(websocketUrl);
                wssv.AddWebSocketService<r2warsWebSocket>("/r2wars");
                wssv.Start();
                while (!ct.IsCancellationRequested) { Thread.Sleep(1000); }
                wssv.Stop();
            }, tokenSource.Token);

            taskA.Start();
            taskB.Start();
            Console.ReadKey();
            tokenSource.Cancel();

        }
    }
}
