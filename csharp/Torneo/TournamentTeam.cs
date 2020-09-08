    using System.Diagnostics;

    /// <summary>
    /// Describes a team that may participate in a tournament.
    /// </summary>
    [DebuggerDisplay("[Team {this.TeamId} @ {this.Rating}]")]
    public sealed class TournamentTeam
    {
        /// <summary>
        /// Holds the unique, application-specific identifier of the team.
        /// </summary>
        private readonly long teamId;

        /// <summary>
        /// Initializes a new instance of the TournamentTeam class.
        /// </summary>
        /// <param name="teamId">The unique, application-specific identifier of the team.</param>
        /// <param name="rating">The team's current rating pertaining to a specific tournament.</param>
        public TournamentTeam(long teamId, int? rating)
        {
            this.teamId = teamId;
            this.Rating = rating;
        }

        /// <summary>
        /// Gets the unique, application-specific identifier of the team.
        /// </summary>
        public long TeamId
        {
            get
            {
                return this.teamId;
            }
        }

        /// <summary>
        /// Gets or sets the team's current rating or seed pertaining to a specific tournament or league.
        /// </summary>
        public int? Rating
        {
            get;
            set;
        }
    }
