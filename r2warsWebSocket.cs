using WebSocketSharp;
using WebSocketSharp.Server;
namespace r2warsTorneo
{
    public class r2warsWebSocket : WebSocketBehavior
    {
        static MyHandler1 h1;
        protected override void OnMessage(MessageEventArgs e)
        {
            string recv = e.Data;
            string msg = "";
            if (recv == "cmd_prevlog")
            {
                r2warsStatic.r2w.prevLog();
            }
            else if (recv == "cmd_nextlog")
            {
                r2warsStatic.r2w.nextLog();
            }
            else if (recv == "cmd_load")
            {
                r2warsStatic.torneo.LoadTournamentPlayers();
            }
            else if (recv == "cmd_run")
            {
                r2warsStatic.torneo.RunTournamentCombats();
            }
            else if (recv == "cmd_stop")
            {
                r2warsStatic.torneo.StopActualCombat();
            }
            else if (recv == "cmd_next")
            {
                r2warsStatic.torneo.StepTournamentCombats();
            }
            else if (recv == "cmd_dbg4")
            {
                r2warsStatic.r2w.bStopAtRoundStart = false;
            }
            else if (recv == "cmd_dbg4si")
            {
                r2warsStatic.r2w.bStopAtRoundStart = true;
            }
            else if (recv == "cmd_dbg5")
            {
                r2warsStatic.r2w.bStopAtRoundEnd = false;
            }
            else if (recv == "cmd_dbg5si")
            {
                r2warsStatic.r2w.bStopAtRoundEnd = true;
            }
            else if (recv == "moreflow")
            {
                r2warsStatic.r2w.sync_var = true;
                msg = "none";
            }
            else if (recv == "arch_arm")
            {
                r2warsStatic.r2w.answer = "arm";
            }
            else if (recv == "arch_x86")
            {
                r2warsStatic.r2w.answer = "x86";
            }
            if (msg!="")
                Send(msg);
        }
        private void R2wars_EventPinta(object sender, MyEvent e)
        {
            r2warsStatic.r2w.sync_var = false;
            Send(e.message);
        }
        protected override void OnError(ErrorEventArgs e)
        {
            base.OnError(e);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            r2warsStatic.r2w.Event_draw -= h1;
            base.OnClose(e);
        }
        protected override void OnOpen()
        {
            //r2warsStatic.r2w.Event_draw -= h1;
            h1 = new MyHandler1(R2wars_EventPinta);
            r2warsStatic.r2w.Event_draw += h1;
            base.OnOpen();
        }
    }
}
