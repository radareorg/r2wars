using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace r2warsTorneo
{
    public class Inicio 
    {
        static List<TournamentTeam> teams = new List<TournamentTeam>();
        static List<TournamentRound> rounds = new List<TournamentRound>();
        static Dictionary<long, string> teamNames = new Dictionary<long, string>();
        static Dictionary<long, string> teamWarriors = new Dictionary<long, string>();
        r2wars r2w;
        RoundRobinPairingsGenerator generator;
        List<TournamentPairing> allcombats = new List<TournamentPairing>();

        TournamentTeamScore[] actualcombatscore = { null, null };
        int ncombat = 0;
        string[] actualcombatnames = { "", "" };
        string[] actualcombatwarriors = { "", "" };

    
        string rutaR2 = "radare2.exe";
        string rutaRabin2 = "rasm2.exe";
        public string textBox1="";
        public string textBox2 ="";
        public Inicio()
        {
            //InitializeComponent();
            if (!OperatingSystem.IsWindows())
            {
                rutaR2 = "r2";
                rutaRabin2 = "rasm2";
            }
        }
        void BuildRounds()
        {
            generator = new RoundRobinPairingsGenerator();
            generator.Reset();
            // Añadimos 10 jugadores
            for (int n = 1; n <= 26; n++)
            {
                var team = new TournamentTeam(n, 0);
                teams.Add(team);
                teamNames.Add(n, string.Format("Player{0}", n));
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

        void loadplayers()
        {
            allcombats.Clear();
            //listBox1.Items.Clear();
            teamNames.Clear();
            teamWarriors.Clear();
            rounds.Clear();
            teams.Clear();
            ncombat = 0;
      
            if (r2w == null)
            {
                r2w = r2warsStatic.r2w;
                r2w.Event_combatEnd += new MyHandler1(CombatEnd);
                r2w.Event_roundEnd += new MyHandler1(RoundEnd);
                r2w.Event_roundExhausted += new MyHandler1(RoundExhausted);
            }
            r2w.nRound = 0;
            r2w.victorias[0] = 0;
            r2w.victorias[1] = 0;
            r2w.bDead = false;
            textBox1 = "";

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
                string tmp = string.Format("Iniciando combate {0} {1} vs {2}", ncombat + 1, actualcombatnames[0], actualcombatnames[1]);
                textBox1 += tmp + Environment.NewLine;
                r2w.playcombat(actualcombatwarriors[0], actualcombatwarriors[1], actualcombatnames[0], actualcombatnames[1], rutaR2, rutaRabin2);
            }
            else
            {
                textBox1+= "end " + DateTime.Now+Environment.NewLine;
                r2w.sendevent(r2w.json_output());
            }
        }

        void drawstats()
        {
            textBox2= string.Format("Combat {0} / {1}", ncombat ,allcombats.Count) + Environment.NewLine;
            var standings = generator.GenerateRankings();

            foreach (var standing in standings)
            {
                string salida = string.Format("{0} {1} {2}", standing.Rank.ToString(), teamNames[standing.Team.TeamId], standing.ScoreDescription);
                textBox2+= salida + Environment.NewLine;
            }
        }

        private void RoundEnd(object sender, MyEvent e)
        {
           
            int nround = e.round + 1;
            textBox1+= "    Round-" + nround.ToString() + " " + r2w.Engine.players[e.ganador].name  + " Wins Cycles:" + e.ciclos.ToString() + Environment.NewLine;
            if (actualcombatscore[e.ganador].Score!=null)
                actualcombatscore[e.ganador].Score+= new HighestPointsScore(1);

        }

        private void RoundExhausted(object sender, MyEvent e)
        {
            int nround = e.round + 1;
            textBox1+= "    Round-" + nround.ToString() + " TIMEOUT Cycles:" + e.ciclos.ToString() + Environment.NewLine;
            r2w.playcombat(actualcombatwarriors[0], actualcombatwarriors[1], actualcombatnames[0], actualcombatnames[1], rutaR2, rutaRabin2);


        }

        private void CombatEnd(object sender, MyEvent e)
        {
            string ganador = "";
            if (r2w.victorias[0] == 2)
                ganador = r2w.Engine.players[0].name;
            if (r2w.victorias[1] == 2)
                ganador = r2w.Engine.players[1].name;
            int ciclos = r2w.totalciclos;
            textBox1+= "Combat Winner: " + ganador + Environment.NewLine;
            ncombat++;
            r2w.victorias[0] = 0;
            r2w.victorias[1] = 0;
            r2w.nRound = 0;
            drawstats();
            runnextcombat();
        }

        public void btLoadPlayer()
        {
            loadplayers();
        }

        public void btRunCombats()
        {
            textBox1 = "start " + DateTime.Now+Environment.NewLine;
            runnextcombat();
        }
    }
}
