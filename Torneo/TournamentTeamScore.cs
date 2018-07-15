    using System;

    /// <summary>
    /// Describes the score that a team has obtained in a tournament.
    /// </summary>
    public sealed class TournamentTeamScore
    {
        /// <summary>
        /// Holds the team being scored.
        /// </summary>
        private readonly TournamentTeam team;

        /// <summary>
        /// Initializes a new instance of the TournamentTeamScore class.
        /// </summary>
        /// <param name="team">The team being scored.</param>
        /// <param name="score">The score that the team obtained.</param>
        public TournamentTeamScore(TournamentTeam team, Score score)
        {
            if (team == null)
            {
                throw new ArgumentNullException("team");
            }

            this.team = team;
            this.Score = score;
        }

        /// <summary>
        /// Gets the team being scored.
        /// </summary>
        public TournamentTeam Team
        {
            get
            {
                return this.team;
            }
        }

        /// <summary>
        /// Gets or sets the score that the team obtained.
        /// </summary>
        public Score Score
        {
            get;
            set;
        }
    }
