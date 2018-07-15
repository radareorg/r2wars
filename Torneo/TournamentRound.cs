    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Describes a round within a tournament which consists of one or more pairings.
    /// </summary>
    public sealed class TournamentRound
    {
        /// <summary>
        /// Holds the list of pairings in the round.
        /// </summary>
        private readonly List<TournamentPairing> pairings;

        /// <summary>
        /// Initializes a new instance of the TournamentRound class.
        /// </summary>
        /// <param name="pairings">The list of pairings in the round.</param>
        public TournamentRound(IEnumerable<TournamentPairing> pairings)
        {
            if (pairings == null)
            {
                throw new ArgumentNullException("pairings");
            }

            this.pairings = new List<TournamentPairing>(pairings);
        }

        /// <summary>
        /// Initializes a new instance of the TournamentRound class.
        /// </summary>
        /// <param name="pairings">The parameter array of pairings in the round.</param>
        public TournamentRound(params TournamentPairing[] pairings)
        {
            if (pairings == null)
            {
                throw new ArgumentNullException("pairings");
            }

            this.pairings = new List<TournamentPairing>(pairings);
        }

        /// <summary>
        /// Gets a shallow read-only copy of the list of pairings in the round.
        /// </summary>
        public IList<TournamentPairing> Pairings
        {
            get
            {
                return this.pairings.AsReadOnly();
            }
        }
    }
