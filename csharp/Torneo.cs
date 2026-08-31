using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace r2warsTorneo
{
    public class Torneo
    {
        static readonly List<TournamentTeam> teams = new List<TournamentTeam>();
        static readonly List<TournamentRound> rounds = new List<TournamentRound>();
        static readonly Dictionary<long, string> teamNames = new Dictionary<long, string>();
        static readonly Dictionary<long, string> teamWarriors = new Dictionary<long, string>();
        static r2wars r2w;

        readonly object lifecycleLock = new object();
        readonly object stateLock = new object();
        readonly List<TournamentPairing> allcombats = new List<TournamentPairing>();
        readonly TournamentTeamScore[] actualcombatscore = { null, null };
        readonly string[] actualcombatnames = { "", "" };
        readonly string[] actualcombatwarriors = { "", "" };

        RoundRobinPairingsGenerator generator;
        Task tournamentTask;
        string[] loadedWarriors = new string[0];
        string loadedArchitecture = "mixed";
        string loadedExtension = ".asm";
        string fullCombatLog = "";
        string actualCombatLog = "";
        string actualDeadReason = "";
        string warriorsDirectory = "warriors";
        string browserWarriorsDirectory = "";
        string workflow = "idle";
        string workflowMessage = "Load warriors to begin.";
        string scores = "No tournament loaded.";
        int ncombat;
        volatile bool tournamentActive;
        volatile bool tournamentAutoRun;
        public volatile bool bCombatEnd = true;

        public Torneo()
        {
            r2w = r2warsStatic.r2w;
            r2w.Event_combatEnd += new MyHandler1(CombatEnd);
            r2w.Event_roundEnd += new MyHandler1(RoundEnd);
            r2w.Event_roundExhausted += new MyHandler1(RoundExhausted);
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => CleanupBrowserWarriors();
        }

        public void SetWarriorsDirectory(string wd)
        {
            warriorsDirectory = wd;
        }

        void SendDrawEvent(string value)
        {
            r2w.send_draw_event(value);
        }

        void SetWorkflow(string value, string message)
        {
            lock (stateLock)
            {
                workflow = value;
                workflowMessage = message;
            }
        }

        string BuildStateJson(string console, string summary)
        {
            lock (stateLock)
            {
                StringBuilder json = new StringBuilder();
                json.Append("{\"workflow\":").Append(JsonUtil.Quote(workflow));
                json.Append(",\"message\":").Append(JsonUtil.Quote(workflowMessage));
                json.Append(",\"status\":").Append(JsonUtil.Quote(workflowMessage));
                json.Append(",\"scores\":").Append(JsonUtil.Quote(scores));
                json.Append(",\"completedCombats\":").Append(ncombat);
                json.Append(",\"totalCombats\":").Append(allcombats.Count);
                if (console != null)
                    json.Append(",\"console\":").Append(JsonUtil.Quote(console));
                if (summary != null)
                    json.Append(",\"summary\":").Append(JsonUtil.Quote(summary));
                json.Append("}");
                return json.ToString();
            }
        }

        public string GetStateJson()
        {
            return BuildStateJson(null, null);
        }

        void SendState(string console = null, string summary = null)
        {
            SendDrawEvent(BuildStateJson(console, summary));
        }

        string BuildScores()
        {
            if (generator == null || teams.Count < 2)
                return "No scores yet.";

            StringBuilder result = new StringBuilder();
            result.Append("STANDINGS — ").Append(ncombat).Append(" / ").Append(allcombats.Count).Append(" battles complete\n\n");
            result.Append("RANK  WARRIOR                       RESULTS\n");
            result.Append("----  ----------------------------  -----------------------------------------\n");
            try
            {
                foreach (TournamentRanking standing in generator.GenerateRankings())
                {
                    string name = teamNames[standing.Team.TeamId];
                    if (name.Length > 28)
                        name = name.Substring(0, 25) + "...";
                    result.Append(standing.Rank.ToString().PadLeft(4)).Append("  ");
                    result.Append(name.PadRight(28)).Append("  ");
                    result.Append(standing.ScoreDescription).Append("\n");
                }
            }
            catch (InvalidTournamentStateException)
            {
                result.Append("Standings are being initialized.\n");
            }
            return result.ToString().TrimEnd();
        }

        private void RoundEnd(object sender, MyEvent e)
        {
            if (!tournamentActive)
                return;
            int round = e.round + 1;
            string roundResult = "    Round " + round + ": " + e.winnername + " wins (" + e.ciclos + " cycles)\n" +
                "      Defeat reason: " + e.loserreason + "\n" +
                "      Instruction: " + e.loserins + "\n";
            fullCombatLog += roundResult;
            actualCombatLog += roundResult;
            actualDeadReason += "Round " + round + " winner: " + e.winnername + "\n" +
                "  Defeated: " + e.losername + "\n" +
                "  Reason: " + e.loserreason + "\n" +
                "  Instruction: " + e.loserins + "\n\n";

            if (e.ganador >= 0 && e.ganador < actualcombatscore.Length && actualcombatscore[e.ganador] != null)
                actualcombatscore[e.ganador].Score += new HighestPointsScore(1);

            scores = BuildScores();
            SendState(actualCombatLog);
        }

        private void RoundExhausted(object sender, MyEvent e)
        {
            if (!tournamentActive)
                return;
            int round = e.round + 1;
            string timeout = "    Round " + round + ": timeout (" + e.ciclos + " cycles)\n";
            actualCombatLog += timeout;
            fullCombatLog += timeout;
            actualDeadReason += timeout;
            SendState(actualCombatLog);
        }

        private void CombatEnd(object sender, MyEvent e)
        {
            if (!tournamentActive)
                return;
            actualCombatLog += "Battle winner: " + e.winnername + "\n";
            fullCombatLog += "Battle winner: " + e.winnername + "\n";
            actualDeadReason += "Winner: " + e.winnername + "\n";
            ncombat++;
            bCombatEnd = true;
            scores = BuildScores();

            if (ncombat >= allcombats.Count)
            {
                FinishTournament();
                return;
            }

            if (!tournamentAutoRun || r2w.bStopAtRoundStart)
            {
                tournamentAutoRun = false;
                SetWorkflow("paused", "Battle complete. Resume for the next battle, or Step through it one cycle at a time.");
            }
            else
            {
                SetWorkflow("running", "Battle complete. Starting the next battle…");
            }
            SendState(actualCombatLog, actualDeadReason);
        }

        bool PrepareNextCombat(bool autoStart)
        {
            lock (lifecycleLock)
            {
                if (!tournamentActive || !bCombatEnd || ncombat >= allcombats.Count)
                    return false;

                int player = 0;
                foreach (TournamentTeamScore teamScore in allcombats[ncombat].TeamScores)
                {
                    actualcombatnames[player] = teamNames[teamScore.Team.TeamId];
                    actualcombatwarriors[player] = teamWarriors[teamScore.Team.TeamId];
                    actualcombatscore[player] = teamScore;
                    actualcombatscore[player].Score += new HighestPointsScore(0);
                    player++;
                }

                actualCombatLog = "Battle " + (ncombat + 1) + " / " + allcombats.Count + ": " +
                    actualcombatnames[0] + " vs " + actualcombatnames[1] + "\n";
                actualDeadReason = actualcombatnames[0] + " vs " + actualcombatnames[1] + "\n\n";
                fullCombatLog += actualCombatLog;
                bCombatEnd = false;

                if (autoStart)
                    SetWorkflow("running", "Battle " + (ncombat + 1) + " / " + allcombats.Count + " is running.");
                else
                    SetWorkflow("paused", "Battle " + (ncombat + 1) + " / " + allcombats.Count + " is ready. Step advances one cycle; Resume runs continuously.");
                SendState(actualCombatLog);

                bool initialized = r2w.playcombat(
                    actualcombatwarriors[0], actualcombatwarriors[1],
                    actualcombatnames[0], actualcombatnames[1], false, autoStart);
                if (!initialized)
                {
                    bCombatEnd = true;
                    tournamentAutoRun = false;
                    SetWorkflow("paused", "The battle could not be initialized. Check the warrior files, then reload them.");
                    SendState();
                }
                return initialized;
            }
        }

        void FinishTournament()
        {
            lock (lifecycleLock)
            {
                if (workflow == "finished")
                    return;
                tournamentAutoRun = false;
                tournamentActive = false;
                bCombatEnd = true;
                fullCombatLog += "Tournament finished " + DateTime.Now + "\n";
                scores = BuildScores();
                SetWorkflow("finished", "Tournament complete. Review the final standings or choose Play again to reset all scores.");
                SendState(fullCombatLog, scores + "\n\nLAST BATTLE\n\n" + actualDeadReason);
                SaveTournamentReport();
            }
        }

        void SaveTournamentReport()
        {
            try
            {
                string filename = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".r2wars.txt";
                using (StreamWriter writer = File.CreateText(filename))
                {
                    writer.WriteLine(scores);
                    writer.WriteLine();
                    writer.WriteLine("FULL LOG");
                    writer.WriteLine("========");
                    writer.Write(fullCombatLog);
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Could not save tournament report: " + exception.Message);
            }
        }

        void TournamentLoop()
        {
            while (tournamentActive)
            {
                if (tournamentAutoRun && bCombatEnd)
                    PrepareNextCombat(true);
                else
                    Thread.Sleep(50);
            }
        }

        bool BeginTournament()
        {
            if (allcombats.Count == 0 || loadedWarriors.Length < 2)
            {
                SetWorkflow("idle", "At least two warriors must be loaded before starting a tournament.");
                SendState();
                return false;
            }
            if (workflow == "finished")
            {
                SetWorkflow("finished", "This tournament is complete. Choose Play again to reset the scores first.");
                SendState();
                return false;
            }
            if (tournamentActive)
                return true;

            fullCombatLog = "Tournament started " + DateTime.Now + "\n";
            tournamentActive = true;
            bCombatEnd = true;
            tournamentTask = Task.Factory.StartNew(TournamentLoop);
            return true;
        }

        public void StopTournament()
        {
            tournamentAutoRun = false;
            tournamentActive = false;
            lock (lifecycleLock)
            {
                r2w.CancelCombat();
                bCombatEnd = true;
            }
            Task task = tournamentTask;
            if (task != null && !task.IsCompleted)
                task.Wait(2000);
        }

        public string getemptymemory()
        {
            StringBuilder memory = new StringBuilder();
            for (int index = 0; index < 1024; index++)
            {
                if (index > 0)
                    memory.Append(',');
                memory.Append("\"\"");
            }
            return memory.ToString();
        }

        private void CreatePairings(string[] selectedfiles, string architecture, string extension)
        {
            r2w.ClearHistory();
            allcombats.Clear();
            teamNames.Clear();
            teamWarriors.Clear();
            rounds.Clear();
            teams.Clear();
            ncombat = 0;
            fullCombatLog = "";
            actualCombatLog = "";
            actualDeadReason = "";
            loadedWarriors = (string[])selectedfiles.Clone();
            loadedArchitecture = architecture;
            loadedExtension = extension;
            generator = new RoundRobinPairingsGenerator();
            generator.Reset();

            for (int index = 0; index < selectedfiles.Length; index++)
            {
                TournamentTeam team = new TournamentTeam(index, 0);
                teams.Add(team);
                string filename = Path.GetFileName(selectedfiles[index]);
                string name = filename.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                    ? filename.Substring(0, filename.Length - extension.Length)
                    : filename;
                teamNames.Add(index, name);
                teamWarriors.Add(index, selectedfiles[index]);
            }

            while (true)
            {
                generator.Reset();
                generator.LoadState(teams, rounds);
                TournamentRound round = generator.CreateNextRound(null);
                if (round == null)
                    break;
                rounds.Add(round);
            }
            foreach (TournamentRound round in rounds)
                foreach (TournamentPairing pairing in round.Pairings)
                    allcombats.Add(pairing);

            string console = "Tournament architecture: " + architecture + "\n" +
                "Warriors directory: " + warriorsDirectory + "\n" +
                "Loaded warriors (" + selectedfiles.Length + "):";
            foreach (string warrior in selectedfiles)
                console += "\n  • " + Path.GetFileName(warrior);

            if (selectedfiles.Length < 2)
            {
                scores = "No tournament loaded.";
                SetWorkflow("idle", "At least two .asm warriors are required. Add warriors, then load again.");
                console += "\n\nAt least two warriors are required to start a tournament.";
            }
            else
            {
                scores = BuildScores();
                SetWorkflow("ready", selectedfiles.Length + " warriors loaded for " + allcombats.Count + " battles. Start the tournament when ready.");
                console += "\n\nReady for " + allcombats.Count + " round-robin battles.";
            }

            string resetDisplay = "{\"player1\":{\"regs\":\"\",\"code\":\"\",\"name\":\"Player 1\"}," +
                "\"player2\":{\"regs\":\"\",\"code\":\"\",\"name\":\"Player 2\"}," +
                "\"memory\":[" + getemptymemory() + "],\"activePlayer\":-1," +
                "\"historyPosition\":0,\"historyCount\":0,\"canBrowseEarlier\":false,\"canBrowseLater\":false}";
            SendDrawEvent(resetDisplay);
            SendState(console);
        }

        public void LoadTournamentPlayers()
        {
            StopTournament();
            CleanupBrowserWarriors();
            string[] files;
            try
            {
                files = Directory.GetFiles(warriorsDirectory)
                    .Where(path => path.EndsWith(".asm", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception exception)
            {
                loadedWarriors = new string[0];
                scores = "No tournament loaded.";
                SetWorkflow("idle", "The warriors directory could not be read: " + exception.Message);
                SendDrawEvent("nowarriors");
                SendState();
                return;
            }
            CreatePairings(files, "mixed (detected from each filename)", ".asm");
        }

        public List<BrowserWarriorSource> GetWarriorSources()
        {
            string[] files = loadedWarriors.Where(File.Exists).ToArray();
            if (files.Length == 0)
            {
                if (!Directory.Exists(warriorsDirectory))
                    return new List<BrowserWarriorSource>();
                files = Directory.GetFiles(warriorsDirectory)
                    .Where(path => path.EndsWith(".asm", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            return files.Select(path => new BrowserWarriorSource
            {
                Name = Path.GetFileName(path),
                Source = File.ReadAllText(path)
            }).ToList();
        }

        public void LoadTournamentSources(IReadOnlyList<BrowserWarriorSource> warriors)
        {
            if (warriors == null)
                throw new ArgumentNullException(nameof(warriors));
            if (warriors.Count > 128)
                throw new InvalidOperationException("A tournament cannot contain more than 128 bots.");

            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (BrowserWarriorSource warrior in warriors)
            {
                string name = (warrior?.Name ?? "").Trim();
                if (name.Length == 0 || name != Path.GetFileName(name) || name.Contains('/') || name.Contains('\\') ||
                    name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new InvalidOperationException("Bot filenames must not contain a directory path.");
                if (!name.EndsWith(".asm", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(name + ": filename must end in .asm");
                if (WarriorCompiler.ArchitectureFromName(name) == r2archs.eArch.unknown)
                    throw new InvalidOperationException(name + ": architecture must be encoded in the filename");
                if (!names.Add(name))
                    throw new InvalidOperationException("Duplicate bot filename: " + name);
                if (string.IsNullOrWhiteSpace(warrior.Source))
                    throw new InvalidOperationException(name + ": source is empty");
            }

            StopTournament();
            string directory = Path.Combine(Path.GetTempPath(), "r2wars-web-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string[] files = warriors
                    .OrderBy(warrior => warrior.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(warrior =>
                    {
                        string path = Path.Combine(directory, warrior.Name.Trim());
                        File.WriteAllText(path, warrior.Source);
                        return path;
                    }).ToArray();
                CleanupBrowserWarriors();
                browserWarriorsDirectory = directory;
                CreatePairings(files, "mixed (browser sources)", ".asm");
            }
            catch
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                throw;
            }
        }

        private void CleanupBrowserWarriors()
        {
            if (string.IsNullOrEmpty(browserWarriorsDirectory))
                return;
            string directory = browserWarriorsDirectory;
            browserWarriorsDirectory = "";
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public void ResetTournament()
        {
            StopTournament();
            if (loadedWarriors.Length < 2)
            {
                LoadTournamentPlayers();
                return;
            }
            CreatePairings(loadedWarriors, loadedArchitecture, loadedExtension);
        }

        public void StopActualCombat()
        {
            if (workflow != "running")
                return;
            tournamentAutoRun = false;
            lock (lifecycleLock)
                r2w.StopCombate();
            if (workflow != "finished")
            {
                SetWorkflow("paused", "Paused. Resume continuous play or Step exactly one cycle.");
                SendState();
            }
        }

        public void StepTournamentCombats()
        {
            if (workflow != "paused")
                return;
            if (!BeginTournament())
                return;

            tournamentAutoRun = false;
            if (bCombatEnd && !PrepareNextCombat(false))
                return;
            if (!r2w.bThreadIni && r2w.bInCombat)
                r2w.stepCombate();
            if (workflow != "finished")
            {
                SetWorkflow("paused", "Paused after one cycle. Step again or Resume continuous play.");
                SendState();
            }
        }

        public void RunTournamentCombats()
        {
            if (workflow != "ready" && workflow != "paused")
                return;
            if (!BeginTournament())
                return;

            tournamentAutoRun = true;
            SetWorkflow("running", "Tournament running. Pause at any time to inspect or step.");
            SendState();
            if (!bCombatEnd && r2w.bInCombat && !r2w.bThreadIni)
                r2w.iniciaCombate();
        }
    }

    internal static class JsonUtil
    {
        public static string Quote(string value)
        {
            if (value == null)
                return "null";
            StringBuilder result = new StringBuilder(value.Length + 2);
            result.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"': result.Append("\\\""); break;
                    case '\\': result.Append("\\\\"); break;
                    case '\b': result.Append("\\b"); break;
                    case '\f': result.Append("\\f"); break;
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    default:
                        if (character < 32)
                            result.Append("\\u").Append(((int)character).ToString("x4"));
                        else
                            result.Append(character);
                        break;
                }
            }
            result.Append('"');
            return result.ToString();
        }
    }
}
