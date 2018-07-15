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
        static public Inicio ini = new Inicio();
    }
    class Program
    {
        static void Main(string[] args)
        {
            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            var taskA = new Task(() =>
            {
                var nancyHost = new Nancy.Hosting.Self.NancyHost(new Uri("http://localhost:9664"), new CustomBootstrapper());
                nancyHost.Start();
                Console.WriteLine("Web server running...");

                Process.Start("http://localhost:9664");


                while (!ct.IsCancellationRequested) { Thread.Sleep(1000); }
                nancyHost.Stop();
            }, tokenSource.Token);

            var taskB = new Task(() =>
            {
                var wssv = new WebSocketServer("ws://localhost:9966");
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
