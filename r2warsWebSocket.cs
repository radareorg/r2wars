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
            else if (recv == "reset")
            {
                r2warsStatic.r2w.iniciaJugadores("jordi.x86-32", "kios.x86-32","Jordi","Kios" );
                msg = r2warsStatic.r2w.json_output();
            }
            else if (recv == "start")
            {
                r2warsStatic.r2w.iniciaCombate();
                msg = r2warsStatic.r2w.json_output();
            }
            else if (recv == "next")
            {
                r2warsStatic.r2w.stepCombate();
                msg = r2warsStatic.r2w.json_output();
            }
            else if (recv == "reset_tournament")
            {
                //r2warsStatic.r2w.btInit_Click();
                r2warsStatic.torneo.btLoadPlayer();
                r2warsStatic.r2w.initmemoria();
                msg = r2warsStatic.r2w.json_output();
            }
            else if (recv == "start_tournament")
            {
                r2warsStatic.torneo.btRunCombats();
                msg = r2warsStatic.r2w.json_output();
            }
            else if (recv == "stop_tournament")
            {
                r2warsStatic.r2w.stopProcess = true;
                msg = r2warsStatic.r2w.json_output();
            }
            else if(recv=="moreflow")
            {
                r2warsStatic.r2w.sync_var = true;
                msg = "none";
                
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
            
            r2warsStatic.r2w.Event_pinta -= h1;
            h1 = new MyHandler1(R2wars_EventPinta);
            r2warsStatic.r2w.Event_pinta += h1;
            base.OnOpen();
        }
    }
}
