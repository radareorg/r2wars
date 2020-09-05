using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace r2warsTorneo
{
    public class Torneo
    {
        static List<TournamentTeam> teams = new List<TournamentTeam>();
        static List<TournamentRound> rounds = new List<TournamentRound>();
        static Dictionary<long, string> teamNames = new Dictionary<long, string>();
        static Dictionary<long, string> teamWarriors = new Dictionary<long, string>();
        static r2wars r2w = null;

        RoundRobinPairingsGenerator generator;
        List<TournamentPairing> allcombats = new List<TournamentPairing>();
        TournamentTeamScore[] actualcombatscore = { null, null };
        //clsEngine.eArch tournamenArch = clsEngine.eArch.x86;
        Task tournamentTask = null;
        int ncombat = 0;
        string[] actualcombatnames = { "", "" };
        string[] actualcombatwarriors = { "", "" };
        string fullCombatLog ="";
        string actualCombatLog = "";
        string actualDeadReason = "";
        string warriorsDirectory = "warriors";
        bool bTournamenTask = false;
        public bool bCombatEnd = true;
        public bool bWaitToResumeTournament = false;
        bool bTournamentRun = false;

        public Torneo()
        {
            r2w = r2warsStatic.r2w;
        }
        public void SetWarriorsDirectory(string wd) {
            this.warriorsDirectory = wd;
        }
        private void espera(int veces, int pausa = 1)
        {
            Task t = Task.Factory.StartNew(() =>
            {
                /*int n = veces;
                while ((n--) > 0)
                {
                    System.Threading.Thread.Sleep(pausa);
                }*/
                System.Threading.Thread.Sleep(veces);
            });
            t.Wait();
        }
        void SendDrawEvent(string str)
        {
            r2w.send_draw_event(str);
        }
        private void RoundEnd(object sender, MyEvent e)
        {
            int nround = e.round + 1;
            fullCombatLog += "    Round-" + nround.ToString() + " " + e.winnername + " Wins Cycles:" + e.ciclos.ToString() + "\\n";
            fullCombatLog += "     Dead Reason     : " + e.loserreason + "\\n";
            fullCombatLog += "     Dead Instruction: " + e.loserins + "\\n";

            actualCombatLog += "    Round-" + nround.ToString() + " " + e.winnername + " Wins Cycles:" + e.ciclos.ToString() + "\\n";
            actualCombatLog += "     Dead Reason     : " + e.loserreason  + "\\n";
            actualCombatLog += "     Dead Instruction: " + e.loserins + "\\n";

            actualDeadReason += " Round-" + nround.ToString() + " Winner: " + e.winnername + "\\n";
            actualDeadReason += "  Looser     : " + e.losername + "\\n";
            actualDeadReason += "  Dead Reason: " + e.loserreason + "\\n";
            actualDeadReason += "  Dead Ins   : " + e.loserins + "\\n\\n";

            if (actualcombatscore[e.ganador].Score!=null)
                actualcombatscore[e.ganador].Score+= new HighestPointsScore(1);
            string s = "{\"console\":\"" + actualCombatLog + "\"}";
            SendDrawEvent(s);
        }
        private void RoundExhausted(object sender, MyEvent e)
        {
            int nround = e.round + 1;
            actualCombatLog += "    Round-" + nround.ToString() + " TIMEOUT Cycles:" + e.ciclos.ToString() +"\\n";
            fullCombatLog   += "    Round-" + nround.ToString() + " TIMEOUT Cycles:" + e.ciclos.ToString() + "\\n";
            actualDeadReason+= "    Round-" + nround.ToString() + " TIMEOUT Cycles:" + e.ciclos.ToString() + "\\n";
            string s = "{\"console\":\"" + actualCombatLog + "\"}";
            SendDrawEvent(s);
        }
        string getstats()
        {
            string stats = string.Format("Combat {0} / {1}", ncombat, allcombats.Count) + "\\n";
            var standings = generator.GenerateRankings();
            int n = 0;
            string salida = "";
            foreach (var standing in standings)
            {
                if (n == 0)
                    salida = "<font style='color:yellow'>";
                else
                    salida = "";
                salida += string.Format("{0} {1} {2}", standing.Rank.ToString(), teamNames[standing.Team.TeamId], standing.ScoreDescription);
                if (n == 2)
                    salida += "</font>";

                stats += salida + "\\n";
                n++;
            }
            return "{\"scores\":\"" + stats + "\",\"console\":\"" + actualCombatLog + "\"}";
        }
        private void CombatEnd(object sender, MyEvent e)
        {
            string ganador = e.winnername;
            int ciclos = e.ciclos;
            actualCombatLog += "Combat Winner: " + ganador + "\\n";
            fullCombatLog += "Combat Winner: " + ganador + "\\n";
            ncombat++;
            SendDrawEvent(getstats());
            actualDeadReason += "Winner: " + ganador + "\\n";
            string s = "{\"infodead\":\"" + actualDeadReason + "\"}";
            SendDrawEvent(s);

            r2warsStatic.r2w.sync_var = false;


            Console.WriteLine("Showing Summary ....");
            while (r2warsStatic.r2w.sync_var == false)
            {
                SendDrawEvent("on");
                //Thread.Sleep(200);
                espera(200);
            }
            //espera(3000);
            espera(6000);

            r2warsStatic.r2w.sync_var = false;
            while (r2warsStatic.r2w.sync_var == false)
            {
                SendDrawEvent("off");
                //Thread.Sleep(200);
                espera(200);
            }
            Console.WriteLine("Hidding Summary ....");
            r2warsStatic.r2w.sync_var = false;
            if (r2w.bStopAtRoundStart == false)
                bCombatEnd = true;
            else
                bWaitToResumeTournament = true;
        }
        void runnextcombat()
        {
            if (ncombat < allcombats.Count)
            {
                int j = 0;
                foreach (var teamScore in allcombats[ncombat].TeamScores)
                {
                    actualcombatnames[j] = teamNames[teamScore.Team.TeamId];
                    actualcombatwarriors[j] = teamWarriors[teamScore.Team.TeamId];
                    actualcombatscore[j] = teamScore;
                    actualcombatscore[j].Score += new HighestPointsScore(0);
                    j++;
                }
                string tmp = string.Format("Combat initialized {0} {1} vs {2}", ncombat + 1, actualcombatnames[0], actualcombatnames[1]);
                actualCombatLog = tmp + "\\n";
                actualDeadReason = string.Format("{0} vs {1}\\n\\n", actualcombatnames[0], actualcombatnames[1]);
                fullCombatLog += tmp + "\\n";
                bCombatEnd = false;
                string s = "{\"console\":\"" + actualCombatLog + "\"}";
                SendDrawEvent(s);
                r2w.playcombat(actualcombatwarriors[0], actualcombatwarriors[1], actualcombatnames[0], actualcombatnames[1], false);
            }
            else
            {
                fullCombatLog += "Tournament end " + DateTime.Now + "\\n";
                bTournamentRun = false;
                string j = getstats().Replace("scores", "infodead").Replace("}", "");
                string s = ",\"console\":\"" + fullCombatLog + "\"}";
                j += s;
                // al ser el final del torneo imprimimos en el resumen la clasificacion final 
                SendDrawEvent(j);
                r2warsStatic.r2w.sync_var = false;
                Console.WriteLine("Showing Final Result.");
                while (r2warsStatic.r2w.sync_var == false)
                {
                    SendDrawEvent("on");
                    espera(200);
                    //Thread.Sleep(200);
                }

                // generamos el fichero de info
                string filename = "";
                filename = string.Format("{0}.r2wars.txt", DateTime.Now.ToString().Replace("/","-").Replace(":","-"));
                using (StreamWriter sw = File.CreateText(filename))
                {
                    sw.WriteLine("RANKING");
                    sw.WriteLine("==============");
                    string stats = string.Format("Combat {0} / {1}", ncombat, allcombats.Count) + Environment.NewLine+ Environment.NewLine;
                    var standings = generator.GenerateRankings();
                    foreach (var standing in standings)
                    {
                        string salida = string.Format("{0} {1} {2}", standing.Rank.ToString(), teamNames[standing.Team.TeamId], standing.ScoreDescription);
                        stats += salida + Environment.NewLine;
                    }
                    sw.Write(stats);
                    sw.WriteLine("");
                    sw.WriteLine("FULL LOG");
                    sw.WriteLine("==============");
                    sw.Write(fullCombatLog.Replace("\\n", Environment.NewLine));
                }

            }
        }
        public void StopTournament()
        {
            while (bTournamenTask == true)
            {
                bTournamentRun = false;
                bCombatEnd = true;
                Thread.Sleep(100);
            }
            r2w.StopCombate();
        }
        public string getemptymemory()
        {
            string res = "";
            for (int x = 0; x < 1024; x++)
                res += "\"\"" + ",";
            res = res.Remove(res.Length - 1);
            return res;
        }
        private void dopairs(string[] selectedfiles, string strarch, string extension)
        {
            allcombats.Clear();
            teamNames.Clear();
            teamWarriors.Clear();
            rounds.Clear();
            teams.Clear();
            ncombat = 0;
            fullCombatLog = "";
            generator = new RoundRobinPairingsGenerator();
            generator.Reset();
            int n = 0;
            foreach (string s in selectedfiles)
            {
                var team = new TournamentTeam(n, 0);
                teams.Add(team);
                string tmp = Path.GetFileName(selectedfiles[n]);
                teamNames.Add(n, tmp.Substring(0, tmp.IndexOf(extension)));
                teamWarriors.Add(n, selectedfiles[n]);
                n++;
            }
            while (true)
            {
                TournamentRound round = null;
                generator.Reset();
                generator.LoadState(teams, rounds);
                round = generator.CreateNextRound(null);
                if (round != null)
                {
                    rounds.Add(round);
                }
                else
                {
                    break;
                }
            }
            foreach (TournamentRound round in rounds)
            {
                foreach (var pairing in round.Pairings)
                {
                    allcombats.Add(pairing);
                }
            }
            string memoria = getemptymemory();
            string salida = "";
            salida = "Tournament arch: " + strarch + "\nTotal Warriors loaded " + selectedfiles.Count().ToString();
            if (selectedfiles.Count() < 2)
            {
                salida += "\nCannot begin Tournament with only one Warrior!";
            }
            else
            {
                salida += "\nPress 'start' button to begin Tournament.";
            }
            string envio = "{\"player1\":{\"regs\":\" \",\"code\":\" \",\"name\":\"Player - 1\"},\"player2\":{\"regs\":\" \",\"code\":\" \",\"name\":\"Player - 2\"},\"memory\":[" + memoria + "],\"console\":\"" + salida + "\",\"status\":\"Warriors Loaded.\",\"scores\":\" \"}";
            SendDrawEvent(envio.Replace("\n", "\\n").Replace("\r", ""));

        }
        public void LoadTournamentPlayers()
        {
            StopTournament();
            if (bTournamentRun == false)
            {
                if (r2w != null)
                {
                    r2w.Event_combatEnd -= new MyHandler1(CombatEnd);
                    r2w.Event_combatEnd += new MyHandler1(CombatEnd);

                    r2w.Event_roundEnd -= new MyHandler1(RoundEnd);
                    r2w.Event_roundEnd += new MyHandler1(RoundEnd);

                    r2w.Event_roundExhausted -= new MyHandler1(RoundExhausted);
                    r2w.Event_roundExhausted += new MyHandler1(RoundExhausted);
                }
                string noWarriors = "Warriors not found. Please copy '.x86-32' or '.arm-32' warriors inside 'warriors' folder.";
                string[] files = new string[] { };
                try
                {
                    files = Directory.GetFiles(warriorsDirectory);
                }
                catch
                {
                    SendDrawEvent("nowarriors");
                    Console.WriteLine(noWarriors);
                    return;
                }

                string[] selectedfiles = files.Where(p => p.EndsWith(".asm")).ToArray();
                string extension = ".asm";
                string strarch = "mixed";
                dopairs(selectedfiles, strarch, extension);
            }  
        }
        public void StopActualCombat()
        {
            r2w.StopCombate();
        }
        public void StepTournamentCombats()
        {
            if (bWaitToResumeTournament)
            {
                bWaitToResumeTournament = false;
                bCombatEnd = true;
            }
            else if (bTournamentRun == false)
            {
                RunTournamentCombats();
               // r2w.bInCombat = true;
            }
            else if (r2w.bThreadIni == false)
            {
                
                r2w.stepCombate();
            }
        }
        public void RunTournamentCombats()
        {
            if (bWaitToResumeTournament)
            {
                bWaitToResumeTournament = false;
                bCombatEnd = true;
            }
            else if (bTournamentRun == false)
            {
                fullCombatLog = "Tournament start " + DateTime.Now + "\\n";
                bTournamentRun = true;
                tournamentTask = Task.Factory.StartNew(() =>
                {
             
                    bTournamenTask = true;
                    System.Diagnostics.Debug.WriteLine("TournamenTask: Ini.");
                    while (bTournamentRun)
                    {
                        if (bCombatEnd == true)
                        {
                            runnextcombat();
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                    bTournamenTask = false;
                    System.Diagnostics.Debug.WriteLine("TournamenTask: Fin.");
                });
            }
            else if (r2w.bThreadIni == false)
                r2w.iniciaCombate();
        }
    }
}
