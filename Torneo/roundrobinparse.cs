    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class RoundRobinPairingsGenerator 
    {
        private class RRPairing
        {
            public TournamentTeam A { get; set; }
            public TournamentTeam B { get; set; }
        }

        private class RRWinRecord : IComparable
        {
            public int Wins
            {
                get;
                set;
            }
            public int Losses
            {
                get;
                set;
            }
            public int Draws
            {
                get;
                set;
            }
            public Score OverallScore
            {
                get;
                set;
            }
            public double WinRecord
            {
                get
                {
                    return (this.Wins * 1.0) + (this.Draws * 0.5) + (this.Losses * 0.0);
                }
            }

            public override bool Equals(object obj)
            {
                RRWinRecord record = obj as RRWinRecord;
                if (record != null)
                {
                    return this == record;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return this.Wins + 10 * this.Draws + 100 * this.Losses;
            }

            public int CompareTo(object obj)
            {
                if (obj == null)
                    return 1;

                RRWinRecord record = obj as RRWinRecord;

                if (record != null)
                {
                    int comp = this.WinRecord.CompareTo(record.WinRecord);
                    if (comp != 0)
                    {
                        return comp;
                    }

                    comp = this.Wins.CompareTo(record.Wins);
                    if (comp != 0)
                    {
                        return comp;
                    }

                    comp = this.Losses.CompareTo(record.Losses);
                    if (comp != 0)
                    {
                        return -1 * comp;
                    }
                    if (record.OverallScore != null)
                    {
                        comp = this.OverallScore.CompareTo(record.OverallScore);
                        if (comp != 0)
                        {
                            return comp;
                        }
                    }
                    return 0;
                }
                else
                {
                    throw new ArgumentException("Object is not an RRWinRecord", "obj");
                }
            }

            public static bool operator ==(RRWinRecord score1, RRWinRecord score2)
            {
                if (object.ReferenceEquals(score1, score2))
                {
                    return true;
                }
                else if ((object)score1 == null || (object)score2 == null)
                {
                    return false;
                }
                else
                {
                    return score1.Wins == score2.Wins &&
                        score1.Losses == score2.Losses &&
                        score1.Draws == score2.Draws &&
                        score1.OverallScore == score2.OverallScore;
                }
            }

            public static bool operator !=(RRWinRecord score1, RRWinRecord score2)
            {
                return !(score1 == score2);
            }

            public static bool operator >(RRWinRecord score1, RRWinRecord score2)
            {
                return score1.CompareTo(score2) > 0;
            }

            public static bool operator <(RRWinRecord score1, RRWinRecord score2)
            {
                return score1.CompareTo(score2) < 0;
            }

            public static bool operator >=(RRWinRecord score1, RRWinRecord score2)
            {
                return score1.CompareTo(score2) >= 0;
            }

            public static bool operator <=(RRWinRecord score1, RRWinRecord score2)
            {
                return score1.CompareTo(score2) <= 0;
            }
        }

        private class RRRank
        {
            public TournamentTeam Team { get; set; }
            public int Rank { get; set; }
            public RRWinRecord Record { get; set; }
        }

        PairingsGeneratorState state = PairingsGeneratorState.NotInitialized;

        public string Name
        {
            get
            {
                return "Round-robin";
            }
        }

        public PairingsGeneratorState State
        {
            get
            {
                return this.state;
            }
        }

        public bool SupportsLateEntry
        {
            get
            {
                return true;
            }
        }

        List<TournamentTeam> loadedTeams;
        List<RRPairing> loadedPairings;
        List<TournamentRound> loadedRounds;

        public void Reset()
        {
            this.loadedTeams = null;
            this.loadedPairings = null;
            this.loadedRounds = null;
            this.state = PairingsGeneratorState.NotInitialized;
        }

        public void LoadState(IEnumerable<TournamentTeam> teams, IList<TournamentRound> rounds)
        {
            if (teams == null)
            {
                throw new ArgumentNullException("teams");
            }

            if (rounds == null)
            {
                throw new ArgumentNullException("rounds");
            }

            // Load our list of teams.
            List<TournamentTeam> newTeams = new List<TournamentTeam>();
            newTeams.AddRange(teams);

            // Build our total list of pairings.
            List<RRPairing> newPairings = new List<RRPairing>();
            for (int i = 0; i < newTeams.Count; i++)
            {
                for (int j = i + 1; j < newTeams.Count; j++)
                {
                    newPairings.Add(new RRPairing() { A = newTeams[i], B = newTeams[j] });
                }
            }

            // Remove from the pairings list each pairing that has already been executed
            foreach (TournamentRound round in rounds)
            {
                foreach (TournamentPairing pairing in round.Pairings)
                {
                    List<TournamentTeamScore> pair = new List<TournamentTeamScore>(pairing.TeamScores);

                    if (pair.Count > 2)
                    {
                        throw new InvalidTournamentStateException("The rounds alread executed in this tournament make it invalid as a round-robin tournament for the following reason:  There exists a pairing containing more than two competing teams.");
                    }
                    else if (pair.Count <= 1)
                    {
                        continue;
                    }
                    else
                    {
                        Func<RRPairing, bool> filter = (RRPairing p) => (p.A == pair[0].Team && p.B == pair[1].Team) || (p.A == pair[1].Team && p.B == pair[0].Team);
                        RRPairing remove = newPairings.SingleOrDefault(filter);
                        if (remove == null)
                        {
                            if (pair[0].Team == pair[1].Team)
                            {
                                throw new InvalidTournamentStateException("The rounds alread executed in this tournament make it invalid as a round-robin tournament for the following reason:  At lease one pairing has the same team entered twice.");
                            }
                            else if (!newTeams.Contains(pair[0].Team) || !newTeams.Contains(pair[1].Team))
                            {
                                throw new InvalidTournamentStateException("The rounds alread executed in this tournament make it invalid as a round-robin tournament for the following reason:  At lease one who does not belong to the tournament team has been involved in a pairing.");
                            }
                            else
                            {
                                throw new InvalidTournamentStateException("The rounds alread executed in this tournament make it invalid as a round-robin tournament for the following reason:  At lease one pairing has been executed more than once.");
                            }
                        }

                        newPairings.Remove(remove);
                    }
                }
            }

            this.loadedTeams = newTeams;
            this.loadedPairings = newPairings;
            this.loadedRounds = new List<TournamentRound>(rounds);
            this.state = PairingsGeneratorState.Initialized;
        }

        public TournamentRound CreateNextRound(int? places)
        {
            if (this.loadedPairings.Count == 0)
            {
                return null;
            }

            IList<TournamentPairing> pairings = new List<TournamentPairing>(this.GetNextRoundPairings(this.loadedPairings));
            return new TournamentRound(pairings);
        }

        private IEnumerable<TournamentPairing> GetNextRoundPairings(List<RRPairing> allPairingsLeft)
        {
            List<RRPairing> pairingsLeft = new List<RRPairing>(allPairingsLeft);
            List<TournamentTeam> teamsAdded = new List<TournamentTeam>();

            while (pairingsLeft.Count > 0)
            {
                var nextPairings = from p in pairingsLeft
                                   orderby
                                        Math.Min(
                                            (from p1 in pairingsLeft
                                             where p1.A == p.A || p1.B != p.A
                                             select p1).Count(),
                                            (from p1 in pairingsLeft
                                             where p1.A == p.B || p1.B != p.B
                                             select p1).Count()
                                         ) descending
                                   select p;

                RRPairing pairing = nextPairings.First();

                yield return new TournamentPairing(
                    new TournamentTeamScore[]
                    {
                        new TournamentTeamScore(pairing.A, null),
                        new TournamentTeamScore(pairing.B, null)
                    });

                List<RRPairing> invalidated = new List<RRPairing>(pairingsLeft.Where(p => (p.A == pairing.A || p.B == pairing.A || p.A == pairing.B || p.B == pairing.B)));
                foreach (RRPairing remove in invalidated)
                {
                    pairingsLeft.Remove(remove);
                }

                teamsAdded.Add(pairing.A);
                teamsAdded.Add(pairing.B);
            }
        }

        public IEnumerable<TournamentRanking> GenerateRankings()
        {
            if (this.loadedPairings.Count > 0)
            {
                throw new InvalidTournamentStateException("The tournament is not in a state that allows ranking for the following reason: There is at least one pairing left to execute.");
            }

            Dictionary<TournamentTeam, RRWinRecord> records = new Dictionary<TournamentTeam, RRWinRecord>();
            foreach (TournamentTeam team in this.loadedTeams)
            {
                records[team] = new RRWinRecord() { Wins = 0, Losses = 0, Draws = 0, OverallScore = null };
            }

            foreach (TournamentRound round in loadedRounds)
            {
                foreach (TournamentPairing pairing in round.Pairings)
                {
                    List<TournamentTeamScore> pair = new List<TournamentTeamScore>(pairing.TeamScores);

                    if (pair.Count <= 1)
                    {
                        continue;
                    }

                    TournamentTeam teamA = pair[0].Team;
                    var scoreA = pair[0].Score;
                    TournamentTeam teamB = pair[1].Team;
                    var scoreB = pair[1].Score;

                    if (scoreA == null || scoreB == null)
                    {
                        //throw new InvalidTournamentStateException("The tournament is not in a state that allows ranking for the following reason: At least one pairing is missing a score.");
                        continue;
                    }

                    records[teamA].OverallScore += scoreA;
                    records[teamB].OverallScore += scoreB;

                    if (scoreA == scoreB)
                    {
                        records[teamA].Draws += 1;
                        records[teamB].Draws += 1;
                    }
                    else if (scoreA > scoreB)
                    {
                        records[teamA].Wins += 1;
                        records[teamB].Losses += 1;
                    }
                    else
                    {
                        records[teamA].Losses += 1;
                        records[teamB].Wins += 1;
                    }
                }
            }

            int r = 1, lastRank = 1;
            RRWinRecord lastRecord = null;

            var ranks = from team in records.Keys
                        let teamRecord = records[team]
                        orderby teamRecord descending
                        select new RRRank() { Team = team, Rank = r++, Record = teamRecord };

            foreach (var rank in ranks)
            {
                if (rank.Record != null && lastRecord == rank.Record)
                {
                    rank.Rank = lastRank;
                }

                lastRecord = rank.Record;
                lastRank = rank.Rank;

                string scoreDescription = String.Format("{0:F} ({1}-{2}-{3} with {4} overall).", rank.Record.WinRecord, rank.Record.Wins, rank.Record.Draws, rank.Record.Losses, rank.Record.OverallScore);
                yield return new TournamentRanking(rank.Team, rank.Rank, scoreDescription);
            }

            yield break;
        }
    }

