using WebSocketSharp;
using WebSocketSharp.Server;
namespace r2warsTorneo
{
    public class r2warsWebSocket : WebSocketBehavior
    {
        
        protected override void OnMessage(MessageEventArgs e)
        {
            string recv = e.Data;
            string msg = "";
            if (recv == "refresh")
            {
                msg = r2warsStatic.r2w.json_output();
            }
            else if (recv == "prevlog")
            {
                r2warsStatic.r2w.prevLog();
                
                msg = r2warsStatic.r2w.json_output(0);
            }
            else if (recv == "nextlog")
            {
                r2warsStatic.r2w.nextLog();
                msg = r2warsStatic.r2w.json_output(0);
            }
            else if (recv == "next")
            {
                r2warsStatic.torneo.StepTournamentCombats();
                //msg = r2warsStatic.r2w.json_output();
            }
            else if (recv == "reset_tournament")
            {
                r2warsStatic.torneo.LoadTournamentPlayers();
            }
            else if (recv == "start_tournament")
            {
                r2warsStatic.torneo.RunTournamentCombats(true);
            }
            else if (recv == "stop_tournament")
            {
                r2warsStatic.torneo.StopActualCombat();
            }
            else if(recv=="moreflow")
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
        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
        }
        static MyHandler1 h1; 
        protected override void OnOpen()
        {
            
            r2warsStatic.r2w.Event_draw -= h1;
            h1 = new MyHandler1(R2wars_EventPinta);
            r2warsStatic.r2w.Event_draw += h1;
            base.OnOpen();
        }
    }
}
