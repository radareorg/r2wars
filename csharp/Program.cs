using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace r2warsTorneo
{
    static public class r2warsStatic
    {
        static public r2wars r2w = new r2wars();
        static public Torneo torneo = new Torneo();
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            string listenAddress = OperatingSystem.IsWindows() ? "127.0.0.1" : "0.0.0.0";
            string httpUrl = Environment.GetEnvironmentVariable("R2WARS_HTTP_URL") ??
                "http://" + listenAddress + ":9664";
            string websocketUrl = Environment.GetEnvironmentVariable("R2WARS_WEBSOCKET_URL") ??
                "http://" + listenAddress + ":9966";

            if (args.Length > 0)
                r2warsStatic.torneo.SetWarriorsDirectory(args[0]);

            WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
            {
                ContentRootPath = AppContext.BaseDirectory,
                WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
            });
            builder.WebHost.UseKestrel();
            builder.WebHost.UseUrls(httpUrl, websocketUrl);
            builder.Services.AddRouting();

            WebApplication app = builder.Build();
            app.UseWebSockets();
            app.UseStaticFiles();

            app.Map("/r2wars", async context =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("A WebSocket connection is required.");
                    return;
                }

                using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
                await r2warsWebSocket.HandleAsync(socket, context.RequestAborted);
            });

            app.MapFallbackToFile("index.html");

            Console.WriteLine("Web server running at " + httpUrl);
            Console.WriteLine("WebSocket server running at " + websocketUrl.Replace("http://", "ws://") + "/r2wars");
            Console.WriteLine("Press Ctrl-C to stop r2wars.");

            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo(httpUrl)
                {
                    UseShellExecute = true
                });
            }

            await app.RunAsync();
        }
    }
}
