using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
namespace r2warsTorneo
{
    public class r2wars
    {
        private const int MAX_CYCLES = 2000;
        private const int MAX_HISTORY_STATES = 2048;
        private sealed class UiHistoryEntry
        {
            public string Regs1;
            public string Code1;
            public string Regs2;
            public string Code2;
            public string Memory;
            public string Status;
            public int ActivePlayer;
        }

        public clsEngine Engine = null;
        public bool bThreadIni = false;
        public bool bStopProcess = false;
        public bool bInCombat = false;
        public bool sync_var = false;
        public bool bStopAtRoundEnd = false;
        public bool bStopAtRoundStart = false;
        public event MyHandler1 Event_draw;
        public event MyHandler1 Event_roundEnd;
        public event MyHandler1 Event_combatEnd;
        public event MyHandler1 Event_roundExhausted;
        public string answer = "";
        string[] pColor = { "b", "r" };
        string[] cRead =  { "q", "y" };
        string[] cWrite = { "v", "o" };
        string[] rr = { "", "" };
        string[] dd = { "", "" };
        string[] memoria = new string[1024];
        readonly object historyLock = new object();
        readonly List<UiHistoryEntry> uiHistory = new List<UiHistoryEntry>();
        int historyIndex = -1;
        string status = "Idle";
        int[] victorias = { 0, 0 };
        int totalciclos = 0;
        int nRound = 0;
        public int nExausted = 0;
        bool bDead = false;
        bool bSingleRound = false;
        Task gameLoopTask = null;
        public r2wars()
        {
            if (Engine == null)
                Engine = new clsEngine();
            initmemoria();
        }
        public void send_draw_event(string s)
        {
            // this pause is magic
            //  Firefox can free memory
            //  chrome never consume memory and work smooth on drawing
            Thread.Sleep(5);
            MyEvent e1 = new MyEvent();
            e1.message = s;
            if (Event_draw != null)
            {
                Event_draw(0, e1);
                /*
                Task t = Task.Factory.StartNew(() =>
                {
                    while (!sync_var)
                    {
                        Thread.Sleep(10);
                    }
                });
                t.Wait();
                */
            }
            e1 = null;
        }
        string json_output(UiHistoryEntry historyEntry = null)
        {
            string username1 = Engine.GetUserName(0);
            string username2 = Engine.GetUserName(1);
            string regs1;
            string code1;
            string regs2;
            string code2;
            string memory;
            string outputStatus;
            int activePlayer;
            int historyPosition;
            int historyCount;
            bool canBrowseEarlier;
            bool canBrowseLater;

            if (historyEntry == null)
            {
                lock (rr)
                {
                    regs1 = rr[0];
                    regs2 = rr[1];
                }
                lock (dd)
                {
                    code1 = dd[0];
                    code2 = dd[1];
                }
                memory = getmemoria();
                outputStatus = status;
                activePlayer = Engine != null && bInCombat ? Engine.thisplayer : -1;
            }
            else
            {
                regs1 = historyEntry.Regs1;
                code1 = historyEntry.Code1;
                regs2 = historyEntry.Regs2;
                code2 = historyEntry.Code2;
                memory = historyEntry.Memory;
                outputStatus = historyEntry.Status;
                activePlayer = historyEntry.ActivePlayer;
            }

            lock (historyLock)
            {
                historyCount = uiHistory.Count;
                historyPosition = historyIndex >= 0 ? historyIndex + 1 : 0;
                canBrowseEarlier = historyIndex > 0;
                canBrowseLater = historyIndex >= 0 && historyIndex < uiHistory.Count - 1;
            }
            if (historyEntry != null)
            {
                string location = historyPosition == historyCount ? "Latest state" :
                    "History " + historyPosition + " / " + historyCount;
                outputStatus = location + " — " + outputStatus;
            }

            string salida = "{\"player1\":{\"regs\":" + JsonUtil.Quote(regs1) + ",\"code\":" + JsonUtil.Quote(code1) + ",\"name\":" + JsonUtil.Quote(username1) + "}," +
                "\"player2\":{\"regs\":" + JsonUtil.Quote(regs2) + ",\"code\":" + JsonUtil.Quote(code2) + ",\"name\":" + JsonUtil.Quote(username2) + "}," +
                "\"memory\":[" + memory + "],\"status\":" + JsonUtil.Quote(outputStatus) + ",\"activePlayer\":" + activePlayer.ToString() +
                ",\"historyPosition\":" + historyPosition.ToString() + ",\"historyCount\":" + historyCount.ToString() +
                ",\"canBrowseEarlier\":" + (canBrowseEarlier ? "true" : "false") +
                ",\"canBrowseLater\":" + (canBrowseLater ? "true" : "false") + "}";
            return salida;
        }
        private void PublishCurrentState()
        {
            UiHistoryEntry entry = new UiHistoryEntry();
            lock (rr)
            {
                entry.Regs1 = rr[0];
                entry.Regs2 = rr[1];
            }
            lock (dd)
            {
                entry.Code1 = dd[0];
                entry.Code2 = dd[1];
            }
            entry.Memory = getmemoria();
            entry.Status = status;
            entry.ActivePlayer = Engine != null && bInCombat ? Engine.thisplayer : -1;
            lock (historyLock)
            {
                uiHistory.Add(entry);
                if (uiHistory.Count > MAX_HISTORY_STATES)
                    uiHistory.RemoveAt(0);
                historyIndex = uiHistory.Count - 1;
            }
            send_draw_event(json_output());
        }
        public void ClearHistory()
        {
            lock (historyLock)
            {
                uiHistory.Clear();
                historyIndex = -1;
            }
        }
        public void initmemoria()
        {
            for (int x = 0; x < 1024; x++)
                memoria[x] = "\"\"";
        }
        public string getmemoria()
        {
            lock (memoria)
            {
                string salida = "";
                for (int x = 0; x < 1024; x++)
                    salida += memoria[x] + ",";
                return salida.Remove(salida.Length - 1);
            }

        }
        void pinta(long offset, string c, string s)
        {
            lock (memoria)
            {
                if (offset>=0 && offset<Engine.memsize)
                    memoria[offset] = "\"" + c + s + "\"";
            }
        }
        void pinta(long offset,  string c,string s,int count)
        {
            lock (memoria)
            {
                while ((count--) != 0)
                    if (offset >= 0 && offset < 1024)
                        memoria[offset++] = "\"" + c + s + "\"";
                    //else
                      //  Console.WriteLine("Zascaaaaa");
            }
        }
        public void testpinta()
        {
            initmemoria();
            pinta(1023, "F");
            pinta(0, "F");

            PublishCurrentState();

        }
        void pinta(int offset, string c)
        {
            lock (memoria)
            {
                if (offset >= 0 && offset < Engine.memsize)
                    memoria[offset] = "\"" + c + "\"";
            }
        }
        void pinta(int offset, int count, string c)
        {
            lock (memoria)
            {
                while ((count--) != 0)
                    if (offset >= 0 && offset < 1024)
                        memoria[offset++] = "\"" + c + "\"";
                    //else
                      //  Console.WriteLine("Zascaaaaa");
            }
        }
        void drawplayerturn(int nplayer)
        {
        }
        string padlines(string t,int maxlen=54)
        {
            string b = "";
            string[] lineas = t.Split('\n');
            foreach (string l in lineas)
            {
                if (l.Length > maxlen)
                    b += l.Substring(0, maxlen) + "\n";
                else
                {
                    string pad = "";
                    for (int n = 0; n < maxlen - l.Length; n++)
                        pad += " ";
                    b += l + pad + "\n";
                }
            }
            return b;
        }
        void drawPC(int nplayer)
        {
            long oldPC = Engine.players[nplayer].actual.oldpc;
            if (oldPC >= 0 && oldPC <= Engine.memsize)
            {
                pinta(oldPC, pColor[nplayer], "", Engine.players[nplayer].actual.oldpcsize);
            }
            long aPC = Engine.players[nplayer].actual.ipc();
            // ponemos la X en la posicion nueva
            if (aPC >= 0 && aPC <= Engine.memsize)
            {
                pinta(aPC, pColor[nplayer], "X", Engine.players[nplayer].actual.pcsize);
            }
        }
        void drawscreen(int nplayer)
        {
            // seleccionamos el offset actual y lo pintamos invertido
            string dasm = padlines(Engine.players[nplayer].actual.dasm);
            string actual = padlines(Engine.players[nplayer].actual.ins);
            int i = dasm.IndexOf(actual);
            if (i != -1)
            {
                dasm = dasm.Insert(i + actual.Length, "</span>");
                dasm = dasm.Insert(i, "<span class='s'>");
            }

            lock (dd)
            {
                dd[nplayer] = "Cycles:" + Engine.players[nplayer].actual.cycles.ToString() + "\nActual Instruction:\n " + padlines(Engine.players[nplayer].actual.ins.Substring(16)) + "\n" + dasm;
            }
            lock (rr)
            {
                rr[nplayer] = Engine.players[nplayer].actual.formatregs();
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
        private void resetTablero()
        {
            initmemoria();
            pinta(Engine.GetAddressProgram(0), Engine.GetSizeProgram(0), "b");
            pinta(Engine.GetAddressProgram(1), Engine.GetSizeProgram(1), "r");
        }
        private void update(int n)
        {
            
            if (bDead)
            {
                drawplayerturn(Engine.thisplayer);
            }
            else
            {
                  
                // Dibujamos la info del jugador
                drawscreen(Engine.thisplayer);
                drawPC(Engine.thisplayer);
                // dibujamos la info del nuevo jugador
                drawscreen(Engine.otherplayer);
                drawPC(Engine.otherplayer);
                // ponemos el marco del jugador actual
                drawplayerturn(Engine.otherplayer);
                // Dibujamos los accesos a memoria
                drawmemaccess(Engine.thisplayer);
            }
        PublishCurrentState();
        }
        private void RoundExhausted()
        {
            Debug.WriteLine("RoundExhausted::Invoked.");
            // Actualizamos la pantalla indicando que pinte los programas
            if (Event_roundExhausted != null)
            {
                MyEvent e3 = new MyEvent();
                e3.message = "";
                e3.ganador = 0;
                e3.round = nRound;
                e3.ciclos = totalciclos;
                e3.winnername = "";
                Event_roundExhausted(this, e3);
            }
      
        }
        private void RoundEnd()
        {
            // notificamos fin del round
            Debug.WriteLine("RoundEnd::Invoked.");
            if (Event_roundEnd != null)
            {
                MyEvent e2 = new MyEvent();
                e2.message = "";
                e2.ganador = Engine.thisplayer;
                e2.round = nRound;
                e2.ciclos = totalciclos;
                e2.winnername = Engine.players[Engine.thisplayer].name;

                e2.loserins = Engine.players[Engine.otherplayer].actual.deadins;
                e2.loserreason = Engine.players[Engine.otherplayer].actual.deadinfo;
                e2.losername = Engine.players[Engine.otherplayer].name;


                Event_roundEnd(this, e2);
            }
          
        }
        private void CombatEnd(bool empate=false)
        {
            Debug.WriteLine("CombatEnd::Invoked.");
            status = "Battle complete";
            if (Event_combatEnd != null)
            {
                MyEvent e1 = new MyEvent();
                e1.message = "";
                if (victorias[0] > victorias[1])
                {
                    e1.ganador = 0;
                    e1.perdedor = 1;
                }
                else
                {
                    e1.ganador = 1;
                    e1.perdedor = 0;
                }

                e1.round = nRound;
                e1.ciclos = totalciclos;
                if (!empate)
                {
                    e1.winnername = Engine.players[e1.ganador].name;
                }
                else
                    e1.winnername = "Draw";
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
                //update(1);
                if (bDead)
                {
                    Console.WriteLine("dead");
                }
                else
                {
                    // Dibujamos la info del jugador
                    drawPC(Engine.thisplayer);
                    // dibujamos la info del nuevo jugador
                    drawPC(Engine.otherplayer);
                    // Dibujamos los accesos a memoria
                    drawmemaccess(Engine.thisplayer);
                    drawplayerturn(Engine.otherplayer);
                    drawscreen(Engine.thisplayer);
                    drawscreen(Engine.otherplayer);
                    PublishCurrentState();
                }
                
            }
            else
            {
                //Engine.players[Engine.thisplayer].logAdd(new clsinfo(Engine.players[Engine.thisplayer].actual.pc, Engine.players[Engine.thisplayer].actual.ins, Engine.players[Engine.thisplayer].actual.dasm, Engine.players[Engine.thisplayer].actual.regs, Engine.players[Engine.thisplayer].actual.mem, Engine.players[Engine.thisplayer].actual.cycles + 1, getmemoria()));
                drawplayerturn(Engine.otherplayer);
                drawscreen(Engine.thisplayer);
                drawscreen(Engine.otherplayer);
                PublishCurrentState();
            }
            // here was have a pause on previous versions now its on send_draw_event
            Engine.switchUserIdx();
        }
        public bool iniciaJugadores(string rutaWarrior1, string rutaWarrior2, string nameWarrior1, string nameWarrior2)
        {
            ClearHistory();
            initmemoria();
            string res = Engine.Init(new string[] {
                                               rutaWarrior1,
                                               rutaWarrior2
                                              },
                                 new string[] {
                                               nameWarrior1,
                                               nameWarrior2
                                             }
                                );
            Console.WriteLine("RES = " + res);
            if (res == "OK")
            {
                // seteamos el jugador 1
                Engine.switchUser(1);
                pinta(Engine.GetAddressProgram(), Engine.GetSizeProgram(), "r");
                // dibujamos la pantalla del jugador 1
                drawscreen(1);//, Engine.players[1].actual.ins, Engine.players[1].actual.dasm, Engine.players[1].actual.regs, Engine.players[1].actual.ipc());
                drawPC(1);
                // seteamos el jugador 0
                Engine.switchUser(0);
                pinta(Engine.GetAddressProgram(), Engine.GetSizeProgram(), "b");
                // dibujamos la pantalla del jugador 0 
                drawscreen(0);//, Engine.players[0].actual.ins, Engine.players[0].actual.dasm, Engine.players[0].actual.regs, Engine.players[0].actual.ipc());
                drawPC(0);
                // ponemos el marco en el jugador0
                drawplayerturn(0);
                return true;
            }
            return false;
        }



        bool bResetArena = false;
        public void stepCombate()
        {
            Debug.WriteLine("r2wars:stepCombate");
            status = "Paused — advanced 1 cycle";
            if (bInCombat)
            {

                if (!bDead)
                {
                    if (bResetArena)
                    {
                        bResetArena = false;
                        Engine.ReiniciaGame(true);
                        // Actualizamos la pantalla indicando que pinte los programas
                        resetTablero();
                        drawPC(Engine.thisplayer);
                        drawPC(Engine.otherplayer);
                        drawscreen(Engine.thisplayer);
                        drawscreen(Engine.otherplayer);
                        PublishCurrentState();
                        return;
                    }

                    totalciclos++;
                    if (totalciclos > MAX_CYCLES)
                    {
                        nExausted++;
                        if (nExausted > 2)
                        {
                            bInCombat = false;
                            bThreadIni = false;
                            bStopProcess = false;
                            RoundExhausted();
                            CombatEnd(true);
                            nExausted = 0;
                            return;
                        }
                        else
                        {
                            RoundExhausted();
                            totalciclos = 0;
                            bResetArena = true;
                            return;
                        }
                    }
                    ExecuteRoundInstruction(false);
                }
                else
                {
                    victorias[Engine.thisplayer]++;
                    if (nRound == 3 || victorias[1] == 2 || victorias[0] == 2)
                    {
                        bInCombat = false;
                        bThreadIni = false;
                        bStopProcess = false;
                        RoundEnd();
                        CombatEnd();
                    }
                    else
                    {
                        RoundEnd();
                        bDead = false;
                        totalciclos = 0;
                        bResetArena = true;
                        nRound++;
                    }
                }
            }
        }
        public void iniciaCombate()
        {
            status = "Running";
            bThreadIni = true;
            gameLoopTask = Task.Factory.StartNew(() =>
            {
                // Jugamos el combate mientras no hayan muertos
                Debug.WriteLine("gameLoopTask: Ini.");
                //int nexausted = 0;
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
                       
                        if (bResetArena)
                        {
                            bResetArena = false;
                            Engine.ReiniciaGame(true);
                            // Actualizamos la pantalla indicando que pinte los programas
                            resetTablero();
                            drawPC(Engine.thisplayer);
                            drawPC(Engine.otherplayer);
                            drawscreen(Engine.thisplayer);
                            drawscreen(Engine.otherplayer);
                            PublishCurrentState();
                            if (bStopAtRoundEnd)
                            {
                                bThreadIni = false;
                                return;
                            }
                        }

                        totalciclos++;
                        if (totalciclos > MAX_CYCLES)
                        {
                            nExausted++;
                            if (nExausted > 2)
                            {
                                bInCombat = false;
                                bThreadIni = false;
                                bStopProcess = false;
                                RoundExhausted();
                                CombatEnd(true);
                                nExausted = 0;
                                Debug.WriteLine("gameLoopTask: Fin(draw)");
                                return;
                            }
                            else
                            {
                                RoundExhausted();
                                totalciclos = 0;
                                bResetArena = true;
                                if (bStopAtRoundEnd)
                                {
                                    bThreadIni = false;
                                    return;
                                }
                            }
                        }
                        ExecuteRoundInstruction(false);
                    }

                    victorias[Engine.thisplayer]++;
                    if (nRound == 3 || victorias[1] == 2 || victorias[0] == 2)
                    {
                        bInCombat = false;
                        bThreadIni = false;
                        bStopProcess = false;
                        RoundEnd();
                        CombatEnd();
                        break;
                    }
                    else
                    {
                        RoundEnd();
                        bDead = false;
                        totalciclos = 0;
                        bResetArena = true;
                        nRound++;
                        if (bStopAtRoundEnd)
                        {
                            bThreadIni = false;
                            break;
                        }
                    }
                }
              
                Debug.WriteLine("gameLoopTask: Fin");
            });
        }
        public bool playcombat(string rutaWarrior1, string rutaWarrior2, string nameWarrior1, string nameWarrior2, bool bSingleRound, bool autoStart)
        {
            if (iniciaJugadores(rutaWarrior1, rutaWarrior2, nameWarrior1, nameWarrior2))
            {
                // ejecutamos el combate
                this.bInCombat = true;     // indicamos que estamos en un combate
                this.bSingleRound = bSingleRound; // indicamos que No queremos un unico round
                this.bStopProcess = false;
                this.victorias[0] = 0;
                this.victorias[1] = 0;
                this.nRound = 0;
                this.nExausted = 0;
                this.bDead = false;
                this.totalciclos = 0;
                status = autoStart ? "Running" : "Paused — ready to step";
                PublishCurrentState();
                if (autoStart)
                {
                    iniciaCombate();
                }
                
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
            if (bInCombat)
                status = "Paused";
        }
        public void CancelCombat()
        {
            StopCombate();
            bInCombat = false;
            bStopProcess = false;
            bThreadIni = false;
            status = "Idle";
        }
        public string prevLog()
        {
            UiHistoryEntry entry;
            lock (historyLock)
            {
                if (historyIndex <= 0 || uiHistory.Count == 0)
                    return "";
                historyIndex--;
                entry = uiHistory[historyIndex];
            }
            return json_output(entry);
        }
        public string nextLog()
        {
            UiHistoryEntry entry;
            lock (historyLock)
            {
                if (historyIndex < 0 || historyIndex >= uiHistory.Count - 1)
                    return "";
                historyIndex++;
                entry = uiHistory[historyIndex];
            }
            return json_output(entry);
        }
    }
}
