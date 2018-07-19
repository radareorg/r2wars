using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

public static class OperatingSystem
{
    public static bool IsWindows() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static bool IsMacOS() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public static bool IsLinux() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}

namespace r2warsTorneo
{
    public class r2wars
    {
        public clsEngine Engine = null;
        private Thread workerThread = null;
        private delegate void UpdateStatusDelegate(int n);
        private UpdateStatusDelegate updateStatusDelegate = null;
        bool bThreadIni = false;
        public bool bStopProcess = false;
        public event MyHandler1 Event_draw;
        public event MyHandler1 Event_roundEnd;
        public event MyHandler1 Event_combatEnd;
        public event MyHandler1 Event_roundExhausted;

        string[] pColor = { "b", "r" };
        string[] cRead =  { "q", "y" };
        string[] cWrite = { "v", "o" };

        bool bInCombat = false;
        bool bSingleRound = false;
        //bool fincombate = false;
        public int[] victorias = { 0, 0 };
        public int totalciclos = 0;
        public int nRound = 0;
        public bool bDead = false;
        string[] memoria = new string[1024];
        string[] rr = { "", "" };
        string[] dd = { "", "" };
        string status = "Idle";
        public  bool sync_var = false;
        public r2wars()
        {
            if (Engine == null)
                Engine = new clsEngine();
            initmemoria();
        }

 
        public void send_draw_event(string s)
        {
            MyEvent e1 = new MyEvent();
            e1.message = s;
            if (Event_draw != null)
            {
                Event_draw(0, e1);
                /*Task t = Task.Factory.StartNew(() =>
                {
                    while (!sync_var)
                    {
                        Thread.Sleep(10);
                    }
                });
                t.Wait();*/
            }
            e1 = null;
        }
        public string json_output()
        {
            string torneo =  "Total Ciclos:" + totalciclos+"\\n"+ r2warsStatic.torneo.textBox1.Replace("\n", "\\n").Replace("\r","");
            string clasi = r2warsStatic.torneo.textBox2.Replace("\n", "\\n").Replace("\r", "");
            string username1 = Engine.GetUserName(0);
            string username2 = Engine.GetUserName(1);
            string salida = "{\"player1\":{\"regs\":\"" + rr[0].Replace("\n", "\\n") + "\",\"code\":\"" + dd[0].Replace("\n", "\\n") + "\\n\\n\\n" + clasi + "\",\"name\":\""+username1+"\"},\"player2\":{\"regs\":\"" + rr[1].Replace("\n", "\\n") + "\",\"code\":\"" + dd[1].Replace("\n", "\\n") + "\",\"name\":\""+username2+"\"},\"memory\":[" + getmemoria() + "],\"console\":\""+torneo+"\",\"status\":\"" + status + "\"}";
            return salida;
        }
        public void initmemoria()
        {
            for (int x = 0; x < 1024; x++)
                memoria[x] = "\"\"";
        }
        string getmemoria()
        {
            string salida = "";
            for (int x = 0; x < 1024; x++)
                salida += memoria[x] + ",";
            return salida.Remove(salida.Length - 1);

        }
        void pinta(int offset, string c, string s)
        {
            lock (memoria)
            {
                memoria[offset] = "\"" + c + s + "\"";
            }
        }
        void pinta(int offset, string c)
        {
            lock (memoria)
            {
                memoria[offset] = "\"" + c + "\"";
            }
        }
        void pinta(int offset, int count, string c)
        {
            lock (memoria)
            {
                while ((count--) != 0)
                    memoria[offset++] = "\"" + c + "\"";
            }
        }
        void drawplayerturn(int nplayer)
        {
         
        }
        void drawslogcreen(int nplayer, clsinfo actual)
        {
            /*RichTextBox[] r = { richAsmP0, richAsmP1 };
            Label[] l = { lblRegsP0, lblRegsP1 };
            Label[] lplayer = { lblPlayer0, lblPlayer1 };
            lplayer[nplayer].Text = Engine.GetUserName(nplayer);
            r[nplayer].SelectionColor = Color.FromArgb(0, 192, 0);
            r[nplayer].SelectionBackColor = Color.Black;
       

            string newdasm = "Cycles:" + actual.cycles.ToString() + "\nActual Instruction: \n" + actual.ins + "\n\n" + actual.dasm;
            r[nplayer].Text = newdasm;
            // Pintamos el desensamblado
            int i = newdasm.IndexOf(actual.ins, 35);
            if (i != -1)
            {
                int fin = newdasm.IndexOf("\n", i);
                r[nplayer].SelectionStart = i;
                r[nplayer].SelectionLength = fin - i;
                r[nplayer].SelectionBackColor = Color.FromArgb(0, 192, 0);
                r[nplayer].SelectionColor = Color.White;
            }
            // pintamos los registros
            l[nplayer].Text = actual.formatregs();
           */



        }
        void drawscreen(int nplayer)
        {
            //RichTextBox[] r = { richAsmP0, richAsmP1 };
            //Label[] l = { lblRegsP0, lblRegsP1 };
            //Label[] lplayer = { lblPlayer0, lblPlayer1 };
            string username = Engine.GetUserName(nplayer);
            //lplayer[nplayer].Text = Engine.GetUserName(nplayer);
            //r[nplayer].SelectionColor = Color.FromArgb(0, 192, 0);
            //r[nplayer].SelectionBackColor = Color.Black;
            // seleccionamos el offset actual y lo pintamos invertido
            lock (dd)
            {
                dd[nplayer] = "Cycles:" + Engine.players[nplayer].cycles.ToString() + "\nActual Instruction: \n" + Engine.players[nplayer].actual.ins + "\n\n" + Engine.players[nplayer].actual.dasm;
            }
            //r[nplayer].Text = newdasm;
            // Pintamos el desensamblado
            //int i = newdasm.IndexOf(Engine.players[nplayer].actual.ins, 35);
            //if (i != -1)
            //{
            //  int fin = newdasm.IndexOf("\n", i);
            //  r[nplayer].SelectionStart = i;
            //r[nplayer].SelectionLength = fin - i;
            //r[nplayer].SelectionBackColor = Color.FromArgb(0, 192, 0);
            //r[nplayer].SelectionColor = Color.White;
            //}
            // pintamos los registros
            lock (rr)
            {
                rr[nplayer] = Engine.players[nplayer].actual.formatregs();
            }
            //l[nplayer].Text = Engine.players[nplayer].actual.formatregs();
            if (Engine.players[nplayer].log.Count > 0)
            {
                int oldPC = Engine.players[nplayer].log[Engine.players[nplayer].log.Count - 1].ipc();
                // quitamos la X de la posicion anterior
                if (oldPC >= 0 && oldPC <= Engine.memsize)
                    pinta(oldPC, pColor[nplayer], "");
            }
            int aPC= Engine.players[nplayer].actual.ipc(); 
            // ponemos la X en la posicion nueva
            if (aPC >= 0 && aPC <= Engine.memsize)
            {
                pinta(aPC, pColor[nplayer], "X");
            }
        }
        void drawmemaccess(int nplayer)//Dictionary<int, int> dicMemRead, Dictionary<int, int> dicMemWrite)
        {
            Dictionary<int, int> dicMemRead = Engine.GetMemAccessReadDict(Engine.players[nplayer].actual.mem);
            Dictionary<int, int> dicMemWrite = Engine.GetMemAccessWriteDict(Engine.players[nplayer].actual.mem);

            if (dicMemRead.Count > 0)
            {
                foreach (var i in dicMemRead)
                {
                    if (i.Key >= 0 && i.Key <= Engine.memsize)
                    {
                        for (int x = 0; x < i.Value; x++)
                            pinta(i.Key + x, cRead[nplayer],"R");
                    }
                }
            }

            if (dicMemWrite.Count > 0)
            {
                foreach (var i in dicMemWrite)
                {
                    if (i.Key >= 0 && i.Key <= Engine.memsize)
                    {
                        for (int x = 0; x < i.Value; x++)
                        {
                            pinta(i.Key + x, cWrite[nplayer],"W");
                        }
                    }
                }
            }
        }
    
        private void update(int n)
        {
            if (n == 1)
            {
                if (bDead)
                {
                    // Dibujamos la info del jugador que relizo la instruccion de muerte
                    clsinfo ins = Engine.players[Engine.thisplayer].logGetPrev();
                    drawslogcreen(Engine.thisplayer, ins);
                    drawplayerturn(Engine.thisplayer);
                }
                else
                {
                    drawmemaccess(Engine.thisplayer);
                    // Dibujamos la info del jugador
                    drawscreen(Engine.thisplayer);

                    // dibujamos la info del nuevo jugador
                    drawscreen(Engine.otherplayer);
                    // ponemos el marco del jugador actual
                    drawplayerturn(Engine.otherplayer);
                }
            }
            if (n==0)
            {
                initmemoria();
                pinta(Engine.GetAddressProgram(0), Engine.GetSizeProgram(0), "b");
                pinta(Engine.GetAddressProgram(1), Engine.GetSizeProgram(1), "r");

            }
            send_draw_event(json_output());
        }
        private void RoundExhausted()
        {
            Debug.WriteLine("RoundExhausted::Invoked.");
            // Reiniciamos el juego
            Engine.ReiniciaGame(true);
            // Actualizamos la pantalla indicando que pinte los programas
            update(0);
            bDead = false;
            if (Event_roundExhausted != null)
            {
                MyEvent e3 = new MyEvent();
                e3.message = "";
                e3.ganador = 0;
                e3.round = nRound;
                e3.ciclos = totalciclos;
                Event_roundExhausted(this, e3);
            }
        }
        private bool RoundEnd()
        {
            // notificamos fin del round
            Debug.WriteLine("RoundEnd::Invoked.");
            if (Event_roundEnd != null)
            {
                MyEvent e2 = new MyEvent();
                e2.message = "";
                e2.ganador = Engine.otherplayer;
                e2.round = nRound;
                e2.ciclos = totalciclos;
                Event_roundEnd(this, e2);
            }
            nRound++;
            totalciclos = 0;
            // MessageBox.Show("Gana jugador: " + Engine.players[Engine.otherplayer].name);
            victorias[Engine.otherplayer]++;
            if (nRound == 3 || victorias[1] == 2 || victorias[0] == 2)
            {
                return true;

            }
            else
            {
                // Reiniciamos el juego
                if (!bSingleRound)
                {
                    Engine.ReiniciaGame(true);
                    // Actualizamos la pantalla indicando que pinte los programas
                    update(0);
                }
                return false;
            }
            

        }
        private void CombatEnd()
        {
            Debug.WriteLine("CombatEnd::Invoked.");
            if (Event_combatEnd != null)
            {
                MyEvent e1 = new MyEvent();
                e1.message = "";
                e1.ganador = Engine.otherplayer;
                e1.round = nRound;
                e1.ciclos = totalciclos;
                this.Event_combatEnd(this, e1);
            }
        }
        private void ExecuteRoundInstruction()
        {
            if (Engine.cyleszero())
            {
                // Realizamos el STEP
                bDead = Engine.stepInfoNew();
            }
            else
            {
                Engine.players[Engine.thisplayer].logAdd(new clsinfo(Engine.players[Engine.thisplayer].actual.pc, Engine.players[Engine.thisplayer].actual.ins, Engine.players[Engine.thisplayer].actual.dasm, Engine.players[Engine.thisplayer].actual.regs, Engine.players[Engine.thisplayer].actual.mem, Engine.players[Engine.thisplayer].cycles + 1));
                // hacemos el switch del user sin llamar al pipe es solo para refrescar
                Engine.switchUserIdx();
            }
        }

        void gameloop()
        {
            totalciclos = 0;
            // Jugamos el combate mientras no hayan muertos
            while (bInCombat)
            {
                while (!bDead)
                {
                    if (this.bStopProcess)
                    {
                        bThreadIni = false;
                        bStopProcess = false;
                        this.workerThread.Abort();
                        return;
                    }
                    totalciclos++;
                    if (totalciclos>8000)
                    {
                        RoundExhausted();
                    }
                    ExecuteRoundInstruction();
                    update(1);
                }
                bool bAllRoundEnd = RoundEnd();
                if (bAllRoundEnd || bSingleRound)
                {
                    break;
                }
                else
                    bDead = false;
            }
            bInCombat = false;
            bThreadIni = false;
            bStopProcess = false;
            CombatEnd();
   

        }
        public void stepCombate()
        {
            if (bInCombat)
            {
                if (!bDead)
                {
                    totalciclos++;
                    if (totalciclos > 8000)
                    {
                        RoundExhausted();
                    }
                    ExecuteRoundInstruction();
                    update(1);
                }
                else
                {
                    bool bAllRoundEnd = RoundEnd();
                    if (bAllRoundEnd || bSingleRound)
                    {
                        CombatEnd();
                    }
                }
            }

        }

        public bool iniciaJugadores(string rutaWarrior1, string rutaWarrior2, string nameWarrior1, string nameWarrior2)
        {
            initmemoria();
            string arch = "x86";
            string res = Engine.Init(new string[] {
                                               rutaWarrior1,
                                               rutaWarrior2
                                              },
                                 new string[] {
                                               nameWarrior1,
                                               nameWarrior2
                                             },
                                 arch
                                );
            Console.WriteLine("RES = " + res);
            if (res == "OK")
            {
                // seteamos el jugador 1
                Engine.switchUser(1);
                pinta(Engine.GetAddressProgram(), Engine.GetSizeProgram(), "r");
                // dibujamos la pantalla del jugador 1
                drawscreen(1);//, Engine.players[1].actual.ins, Engine.players[1].actual.dasm, Engine.players[1].actual.regs, Engine.players[1].actual.ipc());
                // seteamos el jugador 0
                Engine.switchUser(0);
                pinta(Engine.GetAddressProgram(), Engine.GetSizeProgram(), "b");
                // dibujamos la pantalla del jugador 0 
                drawscreen(0);//, Engine.players[0].actual.ins, Engine.players[0].actual.dasm, Engine.players[0].actual.regs, Engine.players[0].actual.ipc());
                // ponemos el marco en el jugador0
                drawplayerturn(0);
                return true;
            }
            return false;
        }
       
        public void iniciaCombate()
        {
            if (!bThreadIni)
            {
                bThreadIni = true;
                // Initialise and start worker thread
                updateStatusDelegate = new UpdateStatusDelegate(this.update);
                workerThread = new Thread(new ThreadStart(this.gameloop));
                workerThread.Start();
            }

        }
        public bool playcombat(string rutaWarrior1, string rutaWarrior2, string nameWarrior1, string nameWarrior2, bool bIniciaCombate, bool bSingleRound)
        {
            if (iniciaJugadores(rutaWarrior1, rutaWarrior2, nameWarrior1,nameWarrior2))
            {
                // ejecutamos el combate

                this.bInCombat = true;     // indicamos que estamos en un combate
                this.bSingleRound = bSingleRound; // indicamos que No queremos un unico round
                this.bStopProcess = false;
                this.victorias[0] = 0;
                this.victorias[1] = 0;
                this.nRound = 0;
                this.bDead = false;
                if (bIniciaCombate)
                    iniciaCombate();
                return true;
            }
            return false;
        }

        public void StopCombate()
        {
            if (this.bThreadIni)
                this.bStopProcess = true;
        }


        private void btPrev_Click()
        {
            clsinfo tmp = Engine.players[1].logGetPrev();
            if (tmp != null)
                drawslogcreen(1, tmp);
            tmp = Engine.players[0].logGetPrev();
            if (tmp!=null)
                drawslogcreen(0, tmp);
        }
        private void btNext_Click()
        {
            clsinfo tmp = Engine.players[1].logGetNext();
            if (tmp != null)
                drawslogcreen(1, tmp);
            tmp = Engine.players[0].logGetNext();
            if (tmp != null)
                drawslogcreen(0, tmp);
        }
        private void textBox1_KeyPress() // cmds de log
        {
            /*    if (e.KeyChar == 13)
                {
                    if (textBox1.Text == "clear")
                    {
                        richLog.Text = "";
                    }
                    else if (textBox1.Text == "log0")
                    {
                        richLog.Text += ">log0\n" + Engine.players[0].logGetFull();
                    }
                    else if (textBox1.Text == "log1")
                    {
                        richLog.Text += ">log1\n" + Engine.players[1].logGetFull();
                    }
                    else if (textBox1.Text == "prev0")
                    {
                        clsinfo tmp = Engine.players[0].logGetPrev();
                        drawslogcreen(0, tmp);

                    }
                    else if (textBox1.Text == "next0")
                    {
                        clsinfo tmp = Engine.players[0].logGetNext();
                        drawslogcreen(0, tmp);
                    }

                    else if (textBox1.Text == "prev1")
                    {
                        clsinfo tmp = Engine.players[1].logGetPrev();
                        drawslogcreen(1, tmp);
                    }
                    else if (textBox1.Text == "next1")
                    {
                        clsinfo tmp = Engine.players[1].logGetNext();
                        drawslogcreen(1, tmp);
                    }
                    else if (textBox1.Text == "next")
                    {
                        clsinfo tmp = Engine.players[1].logGetNext();
                        drawslogcreen(1, tmp);
                        tmp = Engine.players[0].logGetNext();
                        drawslogcreen(0, tmp);
                    }
                    else if (textBox1.Text == "prev")
                    {
                        clsinfo tmp = Engine.players[1].logGetPrev();
                        drawslogcreen(1, tmp);
                        tmp = Engine.players[0].logGetPrev();
                        drawslogcreen(1, tmp);
                    }

                    else
                    {
                        string res = Engine.runcommand(textBox1.Text);
                        richLog.Text += string.Format("\n#{0}\n{1}", textBox1.Text, res);


                    }
                    // scroll it automatically
                    richLog.SelectionStart = richLog.Text.Length;
                    richLog.ScrollToCaret();
                    textBox1.Text = "";
                }*/
        }

    }
}
