    using System.Collections.Generic;

    /// <summary>
    /// Describes a pairing between one or more teams in a tournament.
    /// </summary>
    public sealed class TournamentPairing
    {
        /// <summary>
        /// Holds the list of team scores in the pairing.
        /// </summary>
        private readonly List<TournamentTeamScore> teamScores;

        /// <summary>
        /// Initializes a new instance of the TournamentPairing class.
        /// </summary>
        /// <param name="teamScores">The list of teams in this pairing.</param>
        public TournamentPairing(IEnumerable<TournamentTeamScore> teamScores)
        {
            this.teamScores = new List<TournamentTeamScore>(teamScores);
        }

        /// <summary>
        /// Initializes a new instance of the TournamentPairing class.
        /// </summary>
        /// <param name="teamScores">The parameter aray of teams in this pairing.</param>
        public TournamentPairing(params TournamentTeamScore[] teamScores)
        {
            this.teamScores = new List<TournamentTeamScore>(teamScores);
        }

        /// <summary>
        /// Gets a shallow read-only copy of the list of team scores in the pairing.
        /// </summary>
        public IList<TournamentTeamScore> TeamScores
        {
            get
            {
                return this.teamScores.AsReadOnly();
            }
        }
    }
