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
        r2wars r2w = null;
        RoundRobinPairingsGenerator generator;
        List<TournamentPairing> allcombats = new List<TournamentPairing>();

        TournamentTeamScore[] actualcombatscore = { null, null };
        int ncombat = 0;
        string[] actualcombatnames = { "", "" };
        string[] actualcombatwarriors = { "", "" };

        public string fullCombatLog ="";
        public string stats ="";
        public string actualCombatLog = "";

        public bool bTournamentRun = false;
        public Torneo()
        {
            r2w = r2warsStatic.r2w;
        }

        void drawstats()
        {
            stats= string.Format("Combat {0} / {1}", ncombat ,allcombats.Count) + Environment.NewLine;
            var standings = generator.GenerateRankings();

            foreach (var standing in standings)
            {
                string salida = string.Format("{0} {1} {2}", standing.Rank.ToString(), teamNames[standing.Team.TeamId], standing.ScoreDescription);
                stats+= salida + Environment.NewLine;
            }
            r2w.send_draw_event(r2w.json_output());
            r2w.send_draw_event("on");
    
        }

        private void RoundEnd(object sender, MyEvent e)
        {
           
            int nround = e.round + 1;
            fullCombatLog   += "    Round-" + nround.ToString() + " " + r2w.Engine.players[e.ganador].name  + " Wins Cycles:" + e.ciclos.ToString() + Environment.NewLine;
            actualCombatLog += "    Round-" + nround.ToString() + " " + r2w.Engine.players[e.ganador].name + " Wins Cycles:" + e.ciclos.ToString() + Environment.NewLine;

            if (actualcombatscore[e.ganador].Score!=null)
                actualcombatscore[e.ganador].Score+= new HighestPointsScore(1);
            r2w.send_draw_event(r2w.json_output());

            /*Task t = Task.Factory.StartNew(() =>
            {
                int n = 10;
                while ((n--) > 0)
                {
                    System.Threading.Thread.Sleep(100);
                    Console.WriteLine("Round end Waiting ...");
                }
            });
            t.Wait();*/
        }

        private void RoundExhausted(object sender, MyEvent e)
        {
            int nround = e.round + 1;
            actualCombatLog += "    Round-" + nround.ToString() + " TIMEOUT Cycles:" + e.ciclos.ToString() + Environment.NewLine;
            fullCombatLog += "    Round-" + nround.ToString() + " TIMEOUT Cycles:" + e.ciclos.ToString() + Environment.NewLine;
            
        }
        bool bCombatEnd = true;
        private void CombatEnd(object sender, MyEvent e)
        {
            string ganador = "";
            if (r2w.victorias[0] == 2)
                ganador = r2w.Engine.players[0].name;
            if (r2w.victorias[1] == 2)
                ganador = r2w.Engine.players[1].name;
            int ciclos = r2w.totalciclos;
            actualCombatLog += "Combat Winner: " + ganador + Environment.NewLine;
            fullCombatLog   += "Combat Winner: " + ganador + Environment.NewLine;


            ncombat++;
            drawstats();
            Task t = Task.Factory.StartNew(() =>
            {
                int n = 1000;
                while ((n--) > 0)
                {
                    System.Threading.Thread.Sleep(1);
    
                }
            });
            t.Wait();
            r2w.send_draw_event("off");
            bCombatEnd = true;
        }
        void runnextcombat(bool startcombat)
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
                string tmp = string.Format("Iniciando combate {0} {1} vs {2}", ncombat + 1, actualcombatnames[0], actualcombatnames[1]);
                actualCombatLog = tmp + Environment.NewLine;
                fullCombatLog += tmp + Environment.NewLine;
                bCombatEnd = false;

                r2w.playcombat(actualcombatwarriors[0], actualcombatwarriors[1], actualcombatnames[0], actualcombatnames[1], startcombat, false);
            }
            else
            {
                fullCombatLog += "Tournament end " + DateTime.Now + Environment.NewLine;
                bTournamentRun = false;
                r2w.send_draw_event(r2w.json_output());
            }
        }
        public void LoadTournamentPlayers()
        {
            allcombats.Clear();
            teamNames.Clear();
            teamWarriors.Clear();
            rounds.Clear();
            teams.Clear();
            ncombat = 0;
            if (r2w != null)
            {
                r2w.Event_combatEnd -= new MyHandler1(CombatEnd);
                r2w.Event_combatEnd += new MyHandler1(CombatEnd);

                r2w.Event_roundEnd -= new MyHandler1(RoundEnd);
                r2w.Event_roundEnd += new MyHandler1(RoundEnd);

                r2w.Event_roundExhausted -= new MyHandler1(RoundExhausted);
                r2w.Event_roundExhausted += new MyHandler1(RoundExhausted);
            }

            r2w.nRound = 0;
            r2w.victorias[0] = 0;
            r2w.victorias[1] = 0;
            r2w.bDead = false;
            fullCombatLog = "";

            string[] files = Directory.GetFiles(@".");// fbd.SelectedPath);
            string[] a = files.Where(p => p.EndsWith(".x86-32")).ToArray();
            //listBox1.Items.AddRange(a);
            Console.WriteLine("cargados " + a.Count().ToString() + " archivos");
            generator = new RoundRobinPairingsGenerator();
            generator.Reset();
            int n = 0;
            foreach (string s in a)
            {
                var team = new TournamentTeam(n, 0);
                teams.Add(team);
                string tmp = Path.GetFileName(a[n]);
                teamNames.Add(n, tmp.Substring(0, tmp.IndexOf(".x86-32")));
                teamWarriors.Add(n, a[n]);
                n++;
            }
            // generamos todas las rondas.
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
        }
        public void StopTournamentCombats()
        {
            r2w.StopCombate();
        }
        public void StepTournamentCombats()
        {
            if (bTournamentRun == false)
            {
                RunTournamentCombats(false);
                r2w.bInCombat = true;
            }
            else
                r2w.stepCombate();
        }
        public void RunTournamentCombats(bool startcombat)
        {
           
            if (bTournamentRun == false)
            {
                fullCombatLog = "Tournament start " + DateTime.Now + Environment.NewLine;
                bTournamentRun = true;
                Task t = Task.Factory.StartNew(() =>
                {
                    bool firsttime = true;

                    while (bTournamentRun)
                    {
                        if (bCombatEnd == true)
                        {
                            if (firsttime && !startcombat)
                                runnextcombat(false);
                            else
                                runnextcombat(true);
                        }
                        else
                            Thread.Sleep(100);
                    }
                });

               
            }
            else
                r2w.iniciaCombate();
        }
    }
}
