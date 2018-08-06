using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;

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
        public bool bThreadIni = false;
        public bool bStopProcess = false;
        public event MyHandler1 Event_draw;
        public event MyHandler1 Event_roundEnd;
        public event MyHandler1 Event_combatEnd;
        public event MyHandler1 Event_roundExhausted;

        string[] pColor = { "b", "r" };
        string[] cRead =  { "q", "y" };
        string[] cWrite = { "v", "o" };

        public bool bInCombat = false;
        bool bSingleRound = false;

        int totalciclos = 0;
        int[] victorias = { 0, 0 };
        int nRound = 0;
        bool bDead = false;

        string[] memoria = new string[1024];
        string[] rr = { "", "" };
        string[] dd = { "", "" };
        string[] mm = { "", "" };
        string status = "Idle";
        public  bool sync_var = false;
        public string answer = "";
        Task gameLoopTask = null;
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
        public string json_output(int nPlayerLog = -1)
        {
            string username1 = Engine.GetUserName(0);
            string username2 = Engine.GetUserName(1);
            string memoria = "";
            if (nPlayerLog!=-1)
            {
                memoria = mm[nPlayerLog];
            }
            else
                memoria = getmemoria();
            string salida = "{\"player1\":{\"regs\":\"" + rr[0].Replace("\n", "\\n") + "\",\"code\":\"" + dd[0].Replace("\n", "\\n") + "\",\"name\":\"" + username1 + "\"},\"player2\":{\"regs\":\"" + rr[1].Replace("\n", "\\n") + "\",\"code\":\"" + dd[1].Replace("\n", "\\n") + "\",\"name\":\"" + username2 + "\"},\"memory\":[" + memoria + "], \"status\":\"" + status + "\"}";//,\"scores\":\"" + clasi + "\"}";
            return salida;
        }
        public void initmemoria()
        {
            for (int x = 0; x < 1024; x++)
                memoria[x] = "\"\"";
        }
        public string getmemoria()
        {
            string salida = "";
            for (int x = 0; x < 1024; x++)
                salida += memoria[x] + ",";
            return salida.Remove(salida.Length - 1);

        }
        public string getemptymemory()
        {
            string res = "";
            for (int x = 0; x < 1024; x++)
                res += "\"\"" + ",";
            res = res.Remove(res.Length - 1);
            return res;
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
            lock (dd)
            {
                dd[nplayer] = "Cycles:" + actual.cycles.ToString() + "\nActual Instruction: \n" + actual.ins + "\n\n" + actual.dasm;
            }
            lock (rr)
            {
                rr[nplayer] = actual.formatregs();
            }
            lock(mm)
            {
                mm[nplayer] = actual.txtmemoria;
            }


        }
        void drawscreen(int nplayer)
        {
            // seleccionamos el offset actual y lo pintamos invertido
            string dasm = Engine.players[nplayer].actual.dasm;
            int i = dasm.IndexOf(Engine.players[nplayer].actual.ins);
            if (i != -1)
            {
                dasm = dasm.Insert(i + Engine.players[nplayer].actual.ins.Length, "</span>");
                dasm = dasm.Insert(i, "<span class='s'>");
            }

            lock (dd)
            {
                dd[nplayer] = "Cycles:" + Engine.players[nplayer].cycles.ToString() + "\nActual Instruction: \n" + Engine.players[nplayer].actual.ins + "\n\n" + dasm; 
            }
            lock (rr)
            {
                rr[nplayer] = Engine.players[nplayer].actual.formatregs();
            }

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
        void drawmemaccess(int nplayer)
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
                    //drawslogcreen(Engine.thisplayer, ins);
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
            if (Event_roundExhausted != null)
            {
                MyEvent e3 = new MyEvent();
                e3.message = "";
                e3.ganador = 0;
                e3.round = nRound;
                e3.ciclos = totalciclos;
                Event_roundExhausted(this, e3);
            }
            update(0);
            totalciclos = 0;
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

            // MessageBox.Show("Gana jugador: " + Engine.players[Engine.otherplayer].name);
            victorias[Engine.otherplayer]++;
            if (nRound == 3 || victorias[1] == 2 || victorias[0] == 2)
            {
                return true;
            }
            else
            {
                // Reiniciamos el juego
                totalciclos = 0;
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
                e1.winnername = Engine.players[Engine.otherplayer].name;
                this.Event_combatEnd(this, e1);
            }
             
        }

        private void espera(int veces, int pausa = 1)
        {
            Task t = Task.Factory.StartNew(() =>
            {
                int n = veces;
                while ((n--) > 0)
                {
                    System.Threading.Thread.Sleep(pausa);
                }
            });
            t.Wait();
        }
        private void ExecuteRoundInstruction(bool bWait)
        {
            if (Engine.cyleszero())
            {
                // Realizamos el STEP
                bDead = Engine.stepInfoNew(getmemoria());
                update(1);
                if (bWait)
                    espera(2, 1);
            }
            else
            {
                Engine.players[Engine.thisplayer].logAdd(new clsinfo(Engine.players[Engine.thisplayer].actual.pc, Engine.players[Engine.thisplayer].actual.ins, Engine.players[Engine.thisplayer].actual.dasm, Engine.players[Engine.thisplayer].actual.regs, Engine.players[Engine.thisplayer].actual.mem, Engine.players[Engine.thisplayer].cycles + 1, getmemoria()));
            }
            Engine.switchUserIdx();

        }
        public void stepCombate()
        {
            Debug.WriteLine("r2wars:stepCombate");
            if (bInCombat)
            {
                if (!bDead)
                {
                    totalciclos++;
                    if (totalciclos > 8000)
                    {
                        RoundExhausted();
                        return;
                    }
                    ExecuteRoundInstruction(false);
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
  
        public bool iniciaJugadores(string rutaWarrior1, string rutaWarrior2, string nameWarrior1, string nameWarrior2, clsEngine.eArch arch)
        {
            initmemoria();
            //string arch = "x86";
            //string arch = "arm";
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
            gameLoopTask = Task.Factory.StartNew(() =>
            {
                bThreadIni = true;
                // Jugamos el combate mientras no hayan muertos
                Debug.WriteLine("gameLoopTask: Ini.");
                while (bInCombat)
                {
                    while (!bDead)
                    {
                        if (bStopProcess)
                        {
                            bThreadIni = false;
                            bStopProcess = false;
                            Debug.WriteLine("gameLoopTask: Fin (stopprocess).");
                            return;
                        }
                        totalciclos++;
                        if (totalciclos > 8000)
                        {
                            RoundExhausted();
                        }
                        ExecuteRoundInstruction(true);
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
                Debug.WriteLine("gameLoopTask: Fin");

            });
           

        }
        public bool playcombat(string rutaWarrior1, string rutaWarrior2, string nameWarrior1, string nameWarrior2, bool bIniciaCombate, bool bSingleRound, clsEngine.eArch arch)
        {
            if (iniciaJugadores(rutaWarrior1, rutaWarrior2, nameWarrior1, nameWarrior2, arch))
            {
                // ejecutamos el combate

                this.bInCombat = true;     // indicamos que estamos en un combate
                this.bSingleRound = bSingleRound; // indicamos que No queremos un unico round
                this.bStopProcess = false;
                this.victorias[0] = 0;
                this.victorias[1] = 0;
                this.nRound = 0;
                this.bDead = false;
                this.totalciclos = 0;
                if (bIniciaCombate)
                    iniciaCombate();
                return true;
            }
            return false;
        }

        public void StopCombate()
        {
            while (bThreadIni)
            {
                this.bStopProcess = true;
                Thread.Sleep(100);
            }
        }

        public void prevLog()
        {
            clsinfo tmp = Engine.players[1].logGetPrev();
            if (tmp != null)
                drawslogcreen(1, tmp);
            tmp = Engine.players[0].logGetPrev();
            if (tmp!=null)
                drawslogcreen(0, tmp);

        }
        public void nextLog()
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
