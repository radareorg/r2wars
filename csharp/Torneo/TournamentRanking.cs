    /// <summary>
    /// Describes the position of a team in a tournament's rankings.
    /// </summary>
    public sealed class TournamentRanking
    {
        /// <summary>
        /// Holds the team being ranked.
        /// </summary>
        private readonly TournamentTeam team;

        /// <summary>
        /// Holds the rank number of the ranking.
        /// </summary>
        private readonly double rank;

        /// <summary>
        /// Holds the score description or justification of the ranking.
        /// </summary>
        private readonly string scoreDescription;

        /// <summary>
        /// Initializes a new instance of the TournamentRanking class.
        /// </summary>
        /// <param name="team">The team being ranked.</param>
        /// <param name="rank">The actual rank number of the ranking.</param>
        /// <param name="scoreDescription">The score description or justification of the ranking.</param>
        public TournamentRanking(TournamentTeam team, double rank, string scoreDescription)
        {
            this.team = team;
            this.rank = rank;
            this.scoreDescription = scoreDescription;
        }

        /// <summary>
        /// Gets the team being ranked.
        /// </summary>
        public TournamentTeam Team
        {
            get
            {
                return this.team;
            }
        }

        /// <summary>
        /// Gets the rank number of the ranking.
        /// </summary>
        public double Rank
        {
            get
            {
                return this.rank;
            }
        }

        /// <summary>
        /// Gets the score description or justification of the ranking.
        /// </summary>
        public string ScoreDescription
        {
            get
            {
                return this.scoreDescription;
            }
        }
    }
