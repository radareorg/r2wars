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
            if (recv == "prevlog")
            {
                r2warsStatic.r2w.prevLog();
            }
            else if (recv == "nextlog")
            {
                r2warsStatic.r2w.nextLog();
                //r2warsStatic.r2w.testpinta();
            }
            else if (recv == "next")
            {
                r2warsStatic.torneo.StepTournamentCombats();
            }
            else if (recv == "reset_tournament")
            {
                r2warsStatic.torneo.LoadTournamentPlayers();
            }
            else if (recv == "start_tournament")
            {
                r2warsStatic.torneo.RunTournamentCombats();
            }
            else if (recv == "stop_tournament")
            {
                r2warsStatic.torneo.StopActualCombat();
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

            else if (recv == "cmd_dbg1")
            {
                r2warsStatic.torneo.LoadTournamentPlayers();
            }
            else if (recv == "cmd_dbg2")
            {
                r2warsStatic.r2w.bStopAtRoundStart = true;
                r2warsStatic.r2w.bStopAtRoundEnd = true;
                if (r2warsStatic.torneo.bWaitToResumeTournament)
                {
                    r2warsStatic.torneo.bWaitToResumeTournament = false;
                    r2warsStatic.torneo.bCombatEnd = true;
                }
                else
                {
                    r2warsStatic.torneo.RunTournamentCombats();
                }
            }
            else if (recv == "cmd_dbg3")
            {
                r2warsStatic.r2w.bStopAtRoundStart = false;
                r2warsStatic.r2w.bStopAtRoundEnd = false;
                if (r2warsStatic.torneo.bWaitToResumeTournament)
                {
                    r2warsStatic.torneo.bWaitToResumeTournament = false;
                    r2warsStatic.torneo.bCombatEnd = true;

                }
                else
                {

                    r2warsStatic.torneo.RunTournamentCombats();
                }
            }
            else if (recv == "cmd_dbg4")
            {
                r2warsStatic.torneo.StopActualCombat();
            }

            if (msg!="")
                Send(msg);
        }
        private void R2wars_EventPinta(object sender, MyEvent e)
        {
            r2warsStatic.r2w.sync_var = false;
            Send(e.message);
        }
        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
        }
        protected override void OnOpen()
        {
            r2warsStatic.r2w.Event_draw -= h1;
            h1 = new MyHandler1(R2wars_EventPinta);
            r2warsStatic.r2w.Event_draw += h1;
            base.OnOpen();
        }
    }
}
