using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace r2warsTorneo
{
    public static class r2warsWebSocket
    {
        private const int MaxMessageSize = 1024 * 1024;

        public static async Task HandleAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            Channel<string> outgoing = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            MyHandler1 drawHandler = (sender, drawEvent) =>
            {
                r2warsStatic.r2w.sync_var = false;
                outgoing.Writer.TryWrite(drawEvent.message);
            };

            using CancellationTokenSource connectionCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken connectionToken = connectionCancellation.Token;

            r2warsStatic.r2w.Event_draw += drawHandler;
            outgoing.Writer.TryWrite(r2warsStatic.torneo.GetStateJson());

            try
            {
                Task receiveTask = ReceiveLoopAsync(socket, outgoing.Writer, connectionToken);
                Task sendTask = SendLoopAsync(socket, outgoing.Reader, connectionToken);

                await Task.WhenAny(receiveTask, sendTask);
                connectionCancellation.Cancel();
                outgoing.Writer.TryComplete();

                try
                {
                    await Task.WhenAll(receiveTask, sendTask);
                }
                catch (OperationCanceledException) when (connectionToken.IsCancellationRequested)
                {
                }
                catch (WebSocketException)
                {
                    // The peer disconnected without completing a close handshake.
                }
            }
            finally
            {
                r2warsStatic.r2w.Event_draw -= drawHandler;
                outgoing.Writer.TryComplete();
            }
        }

        private static async Task ReceiveLoopAsync(
            WebSocket socket,
            ChannelWriter<string> outgoing,
            CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[4096];

            while (socket.State == WebSocketState.Open)
            {
                using MemoryStream message = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseOutputAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None);
                        return;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.InvalidMessageType,
                            "Only text commands are supported.",
                            cancellationToken);
                        return;
                    }

                    message.Write(buffer, 0, result.Count);
                    if (message.Length > MaxMessageSize)
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.MessageTooBig,
                            "Command is too large.",
                            cancellationToken);
                        return;
                    }
                }
                while (!result.EndOfMessage);

                string response = Dispatch(Encoding.UTF8.GetString(message.ToArray()));
                if (!string.IsNullOrEmpty(response))
                    outgoing.TryWrite(response);
            }
        }

        private static async Task SendLoopAsync(
            WebSocket socket,
            ChannelReader<string> outgoing,
            CancellationToken cancellationToken)
        {
            await foreach (string message in outgoing.ReadAllAsync(cancellationToken))
            {
                byte[] payload = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
            }
        }

        private static string Dispatch(string command)
        {
            switch (command)
            {
                case "cmd_state":
                    return r2warsStatic.torneo.GetStateJson();
                case "cmd_prevlog":
                    return r2warsStatic.r2w.prevLog();
                case "cmd_nextlog":
                    return r2warsStatic.r2w.nextLog();
                case "cmd_load":
                    r2warsStatic.torneo.LoadTournamentPlayers();
                    break;
                case "cmd_reset":
                    r2warsStatic.torneo.ResetTournament();
                    break;
                case "cmd_run":
                    r2warsStatic.torneo.RunTournamentCombats();
                    break;
                case "cmd_stop":
                    r2warsStatic.torneo.StopActualCombat();
                    break;
                case "cmd_step":
                case "cmd_next":
                    r2warsStatic.torneo.StepTournamentCombats();
                    break;
                case "cmd_dbg4":
                    r2warsStatic.r2w.bStopAtRoundStart = false;
                    break;
                case "cmd_dbg4si":
                    r2warsStatic.r2w.bStopAtRoundStart = true;
                    break;
                case "cmd_dbg5":
                    r2warsStatic.r2w.bStopAtRoundEnd = false;
                    break;
                case "cmd_dbg5si":
                    r2warsStatic.r2w.bStopAtRoundEnd = true;
                    break;
                case "moreflow":
                    r2warsStatic.r2w.sync_var = true;
                    return "none";
                case "arch_arm":
                    r2warsStatic.r2w.answer = "arm";
                    break;
                case "arch_x86":
                    r2warsStatic.r2w.answer = "x86";
                    break;
            }

            return null;
        }
    }
}
